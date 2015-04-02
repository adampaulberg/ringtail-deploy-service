-- Written by Greg (Lord Duffcakes) Duffie
--

--C# will automatically connect to master, no need for this
--use master
--go

if object_id('dbo.rp_case_custom_scripts') is not null
begin
    drop procedure dbo.rp_case_custom_scripts
end
go

create procedure dbo.rp_case_custom_scripts
(
     @database_name nvarchar(128)                   -- [Required] Does not have to be the same name as the backup. You can restore a Longford.bak file as a database named "PaulHogan" if you want.
    ,@set_simple_recovery bit = 0                   -- [Optional] Changes the database to simple recovery mode for better performance.
    ,@shrink_log_file bit = 0                       -- [Optional] Shrinks the log file after setting simple recovery to reduce file size on disk.
    ,@set_trustworthy bit = 0                       -- [Optional] Changes the database to trustworthy.
    ,@disable_auditdata_and_fieldlocking bit = 0    -- [Optional] Disables the audit data triggers and field_locking triggers.
    ,@enable_agent_access bit = 0                   -- [Optional] Enables the Validate Agent Access list_variable
    ,@enable_ingestions bit = 0                     -- [Optional] Enables the Ingestions list_variable
    ,@rollback_version_numbers bit = 0              -- [Optional] Will rollback the database version numbers to 8.000.0000 for you.
    ,@rebuild_fulltext_indexes bit = 0              -- [Optional] Fixes problems with full-text population
    ,@reset_all_passwords_to varchar(25) = null     -- [Optional] Resets everyone's password, sets password_changed and last_login_attempt to getdate(), 
    ,@debug tinyint = 0
)
with encryption
as

set nocount on
set xact_abort on
set transaction isolation level read uncommitted

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] START'

/* Suggested @debug values
1 = Simple print statements
2 = Simple select statements (e.g. select @variable_1 as variable_1, @variable_2 as variable_2)
3 = Result sets from temp tables (e.g. select '#temp_table_name' as '#temp_table_name' from #temp_table_name where ...)
4 = @sql statements from exec() or sp_executesql
*/

declare
     @return int = 0
    ,@sql nvarchar(max)
    ,@is_ce bit = 0
    ,@is_dw bit = 0

if not exists (select 1 from sys.databases where name = @database_name)
begin
    raiserror('Database [%s] does not exist.', 16, 1, @database_name)
    return -1
end

if right(@database_name, 3) = '_CE' set @is_ce = 1
if right(@database_name, 3) = '_DW' set @is_dw = 1

--====================================================================================================

if @set_simple_recovery = 1
begin
    if not exists (select 1 from sys.databases where recovery_model = 3 and name = @database_name)
    begin
        if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Changing database [' + @database_name + '] to SIMPLE recovery model'

        select
             @sql = 'ALTER DATABASE [<<@database_name>>] SET RECOVERY SIMPLE WITH NO_WAIT'
            ,@sql = replace(@sql, '<<@database_name>>', @database_name)

        if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

        exec sp_executesql @sql
    end
end

--====================================================================================================

/* This doesn't work yet.

if @shrink_log_file = 1
begin
    if not exists (select 1 from sys.databases where recovery_model = 3 and name = @database_name)
    begin
        if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Shrinking log file on database [' + @database_name + ']'

        select
             @sql = 'DBCC SHRINKFILE (N''<<@log_name>>'', 0, TRUNCATEONLY)'
            ,@sql = replace(@sql, '<<@log_name>>', @log_name)

        if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

        exec sp_executesql @sql
    end
end

*/

--====================================================================================================

if @set_trustworthy = 1
begin
    if not exists (select 1 from sys.databases where is_trustworthy_on = 1 and name = @database_name)
    begin
        if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Setting database [' + @database_name + '] to TRUSTWORTHY'

        select
             @sql = 'ALTER DATABASE [<<@database_name>>] SET TRUSTWORTHY ON'
            ,@sql = replace(@sql, '<<@database_name>>', @database_name)

        if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

        exec sp_executesql @sql
    end
end

--====================================================================================================

if @disable_auditdata_and_fieldlocking = 1 and @is_ce = 0 and @is_dw = 0
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Disabling audit data triggers on database [' + @database_name + ']'

    set @sql = '
    use [<<@database_name>>]

    declare @sql nvarchar(max)

    if exists (select 1 from sys.tables where name = ''fti_tb_AD_xref'')
    begin
        -- Set all of the tables to disabled
        update dbo.fti_tb_AD_xref set state_id = 0
    end

    -- Reset the archive database name to handle cases where the DB was restored as a different name.
    if exists (select 1 from sys.tables where name = ''fti_tb_TL_config'')
    begin
        update dbo.fti_tb_TL_config set config_value = ''<<@database_name>>_Archive'' where config_name = ''archive_db_name''
    end

    if exists (select 1 from sys.databases where name = ''<<@database_name>>_Archive'')
    begin
        -- This should turn off Audit Data
        if exists (select 1 from sys.procedures where name = ''fti_sp_AD_reinitialize_archiving_tables_and_triggers_create'')
        begin
            exec dbo.fti_sp_AD_reinitialize_archiving_tables_and_triggers_create
        end
    end

    -- But just in case the above step did not work...this will drop Audit Data and also Field Locking triggers
    -- You can not just disable the triggers since there are post processing scripts that inadvertantly turn them back on
    while 1=1
    begin
        select top 1 @sql = ''drop trigger '' + name
        from sys.triggers
        where is_disabled = 0 
        and (name like ''fti[_]tr[_]AD[_]%'' or name like ''fti[_]fr[_]FL[_]%'')

        if @@rowcount = 0
            break

        --print @sql

        exec sp_executesql @sql
    end'

    set @sql = replace(@sql, '<<@database_name>>', @database_name)

    if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

    exec sp_executesql @sql
end

--====================================================================================================

if @enable_agent_access = 1 and @is_ce = 0 and @is_dw = 0
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Set ''Validate Agent Path Access'' to False on database [' + @database_name + ']'

    select
         @sql = 'if exists (select 1 from [<<@database_name>>].dbo.list_variables) begin update [<<@database_name>>].dbo.list_variables set thevalue = ''False'' where thelabel = ''Validate Agent Path Access'' end'
        ,@sql = replace(@sql, '<<@database_name>>', @database_name)

    if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

    exec sp_executesql @sql
end

--====================================================================================================

if @enable_ingestions = 1 and @is_ce = 0 and @is_dw = 0
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Set ''Enable_Ingestion'' to True on database [' + @database_name + ']'

    select
         @sql = 'if exists (select 1 from [<<@database_name>>].dbo.list_variables) begin update [<<@database_name>>].dbo.list_variables set theValue = 1 where theLabel = ''Enable_Ingestion'' end'
        ,@sql = replace(@sql, '<<@database_name>>', @database_name)

    if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

    exec sp_executesql @sql

    -- update portal.dbo.list_variables set thevalue = '172.30.99.91' where thelabel = 'IngestionLicensingServer'
end

--====================================================================================================

if @rollback_version_numbers = 1
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Rolling back version numbers to 08.0000.0000 on database [' + @database_name + ']'

    select
        @sql = '
        use [<<@database_name>>];
        
        if exists (select 1 from dbo.list_variables)
        begin
            declare @version varchar(25)
            select @version = thevalue from dbo.list_variables where thelabel = ''dbScriptModel''
            update dbo.list_variables set theValue = replace(thevalue, @version, ''08.0000.0000'') where theLabel in (''dbModel'', ''dbScriptModel'', ''RingtailDatabaseModel'')
        end'
        ,@sql = replace(@sql, '<<@database_name>>', @database_name)

    if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

    exec sp_executesql @sql
end

--====================================================================================================

if @rebuild_fulltext_indexes = 1 and @is_ce = 0 and @is_dw = 0
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Fix full-text population errors on database [' + @database_name + ']'

    select
         @sql = 'use [<<@database_name>>]; if exists (select 1 from sys.fulltext_catalogs where name = ''OtherTables'') begin ALTER FULLTEXT CATALOG OtherTables REBUILD end;'
        ,@sql = replace(@sql, '<<@database_name>>', @database_name)

    if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

    exec sp_executesql @sql
end

if nullif(@reset_all_passwords_to, '') is not null and @is_ce = 0 and @is_dw = 0
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] Reset all passwords on database [' + @database_name + '] to [' + @reset_all_passwords_to + ']'

    select
         @sql = 'use [<<@database_name>>]; if exists (select 1 from sys.tables where name = ''list_users'') update dbo.list_users set password = pwdencrypt(<<@reset_all_passwords_to>>), login_attempts = 0, password_changed = getdate(), last_login_attempt = getdate();'
        ,@sql = replace(@sql, '<<@database_name>>', @database_name)
        ,@sql = replace(@sql, '<<@reset_all_passwords_to>>', @reset_all_passwords_to)

    if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] @sql: ' + isnull(@sql, N'{null}')

    exec sp_executesql @sql
end

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_custom_scripts] END'

return @return

go

grant exec on dbo.rp_case_custom_scripts to public
go

/* DEV TESTING

exec master.dbo.rp_case_custom_scripts
     @database_name = 'RestoreTesting'
    ,@set_simple_recovery = 1
    ,@shrink_log_file = 0
    ,@set_trustworthy = 1
    ,@disable_auditdata_and_fieldlocking = 1
    ,@enable_agent_access = 1
    ,@enable_ingestions = 1
    ,@rollback_version_numbers = 0
    ,@rebuild_fulltext_indexes = 0
    ,@reset_all_passwords_to = '!password1'
    ,@debug = 9

*/
