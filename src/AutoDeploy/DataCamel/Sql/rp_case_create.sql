if object_id('dbo.rp_case_create') is not null
begin
    drop procedure dbo.rp_case_create
end
go

create procedure dbo.rp_case_create
(
     @database_name nvarchar(128)               -- [Required] If the database doesn't exist then we'll call rp_case_create.
    ,@sql_data_directory nvarchar(500) = null   -- [Optional] Will use server default if not passed in.
    ,@sql_log_directory nvarchar(500) = null    -- [Optional] Will use server default if not passed in.
    ,@username nvarchar(128)                    -- [Required] This is needed for the create script.
    ,@ringtail_app_version varchar(25) = null   -- [Optional] If null then we'll use the most recent SQL Components
    ,@debug tinyint = 0
)
with encryption
as

set nocount on
set xact_abort on
set transaction isolation level read uncommitted

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] START'

/* Suggested @debug values
1 = Simple print statements
2 = Simple select statements (e.g. select @variable_1 as variable_1, @variable_2 as variable_2)
3 = Result sets from temp tables (e.g. select '#temp_table_name' as '#temp_table_name' from #temp_table_name where ...)
4 = @sql statements from exec() or sp_executesql
*/

declare
     @return int = 0
    ,@sql_component_path nvarchar(2000)
    ,@sql nvarchar(max)
    ,@default_data_path nvarchar(4000)
    ,@default_log_path nvarchar(4000)
    ,@database_name_ce nvarchar(128) = @database_name + N'_CE'
    ,@database_name_dw nvarchar(128) = @database_name + N'_DW'

-- Handle zero-length (empty) strings
select
     @ringtail_app_version = nullif(@ringtail_app_version, '')
    ,@sql_data_directory = nullif(@sql_data_directory, N'')
    ,@sql_log_directory = nullif(@sql_log_directory, N'')

--====================================================================================================

/* Don't try to run this on a CE or DW database */

if @database_name like '%[_]CE' or @database_name like '%[_]DW'
begin
    set @return = -1
    raiserror('Database [%s] can not be created directly. Create the Case instead and it will create the _CE and _DW databases.', 16, 1, @database_name)
    return @return
end

--====================================================================================================

/* If the database exists we will upgrade it instead. If not, delete any CE or DW databases that may still be around. */

if exists (select 1 from sys.databases where name = @database_name)
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] [' + @database_name + '] already exists. Running rp_case_upgrade.'

    exec master.dbo.rp_case_upgrade
         @database_name = @database_name
        ,@username = @username
        ,@ringtail_app_version = @ringtail_app_version
        ,@debug = @debug

    return @return
end
else
begin
    /* Drop CE and DW databases */

    if exists (select 1 from sys.databases where name = @database_name_ce)
    begin
        if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Running rp_case_drop on database [' + @database_name_ce + ']'

        exec master.dbo.rp_case_drop
             @database_name = @database_name_ce
            ,@debug = @debug
    end
    
    if exists (select 1 from sys.databases where name = @database_name_dw)
    begin
        if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Running rp_case_drop on database [' + @database_name_dw + ']'

        exec master.dbo.rp_case_drop
             @database_name = @database_name_dw
            ,@debug = @debug
    end
end

--====================================================================================================

/* Since @ringtail_app_version is null go find the latest version. */

if @ringtail_app_version is null
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Running rp_get_sql_components.'

    exec master.dbo.rp_get_sql_components
         @sql_component_path = @sql_component_path output
        ,@ringtail_app_version = @ringtail_app_version output
        ,@debug = @debug

    if @sql_component_path is null
    begin
        raiserror('Could not find a valid SQL Component path.', 16, 1)
        return @return
    end
end

--====================================================================================================

if @sql_data_directory is null or @sql_log_directory is null
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Find the default data and log paths'

    exec master.dbo.rp_get_default_database_location
         @default_data_path = @default_data_path output
        ,@default_log_path = @default_log_path output
        ,@debug = @debug

    if @sql_data_directory is null set @sql_data_directory = @default_data_path
    if @sql_log_directory is null set @sql_log_directory = @default_log_path

    if @debug >= 1
    begin
        print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] @sql_data_directory: ' + isnull(@sql_data_directory, N'{null}')
        print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] @sql_log_directory: ' + isnull(@sql_log_directory, N'{null}')
    end
end

--====================================================================================================

/* Create new case */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Creating database [' + @database_name + ']'

select
     @sql = 'create database [<<@database_name>>] on primary (name = ''<<@database_name>>'', filename = N''<<@sql_data_directory>><<@database_name>>.mdf'', size = 262144KB, filegrowth = 20%) log on (name = ''<<@database_name>>_log'', filename = N''<<@sql_log_directory>><<@database_name>>_log.ldf'', size = 131072KB, filegrowth = 20%) with trustworthy on;'
    ,@sql = replace(@sql, '<<@database_name>>', @database_name)
    ,@sql = replace(@sql, N'<<@sql_data_directory>>', master.dbo.rf_directory_slash(null, @sql_data_directory, N'\'))
    ,@sql = replace(@sql, N'<<@sql_log_directory>>', master.dbo.rf_directory_slash(null, @sql_log_directory, N'\'))

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] @sql: ' + isnull(@sql, '{null}')

exec sp_executesql @sql

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Finished creating database [' + @database_name + ']'

--====================================================================================================

/* Install the Bootstrap procedures */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Running rp_install_bootstrap on database [' + @database_name + '] with @ringtail_app_version [' + @ringtail_app_version + ']'

exec master.dbo.rp_install_bootstrap
     @database_name = @database_name
    ,@ringtail_app_version = @ringtail_app_version
    ,@sql_component_path = @sql_component_path
    ,@debug = @debug

--====================================================================================================

/* Install Ringtail scripts */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] Installing Ringtail scripts on database [' + @database_name + ']'

set @sql = N'
use [<<@database_name>>];

declare @dbType varchar(40), @path varchar(400), @reference xml, @ringtailModel varchar(255)

set @reference = ''<reference>
    <targetpath>C:\Program Files\Ringtail\SQL Component_v<<@ringtail_app_version>>\Scripts\Case</targetpath>
    <substitution>
        <s x="$(dbUser)" y="<<@username>>"/>
        <s x="$(mdfSize)" y="250mb"/>
        <s x="$(mdfGrowth)" y="20%"/>
        <s x="$(ldfSize)" y="120mb"/>
        <s x="$(ldfGrowth)" y="20%"/>
        <s x="$(portalAdminUser)" y="admin"/>
        <s x="$(portalAdminPwd)" y="admin"/>
    </substitution>
    </reference>''

exec dbo.rs_sp_database__upgrade @reference

select ''<<@database_name>> after create/install'' as ''<<@database_name>> after create/install'', * from dbo.list_variables where thelabel in (''dbModel'',''dbScriptModel'',''RingtailApplicationVersion'');
'

select
     @sql = replace(@sql, '<<@database_name>>', @database_name)
    ,@sql = replace(@sql, '<<@ringtail_app_version>>', @ringtail_app_version)
    ,@sql = replace(@sql, '<<@username>>', @username)

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] @sql: ' + isnull(@sql, '{null}')

if @debug <> 255
    exec sp_executesql @sql

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_create] END'

return @return

go

grant exec on dbo.rp_case_create to public
go

/* DEV TESTING

exec master.dbo.rp_case_drop
     @database_name = 'eyjnhdafb fsazdf'
    ,@debug = 1

exec master.dbo.rp_case_drop
     @database_name = 'eyjnhdafb fsazdf_DW'
    ,@debug = 1

exec master.dbo.rp_case_drop
     @database_name = 'eyjnhdafb fsazdf_CW'
    ,@debug = 1

exec master.dbo.rp_case_create
     @database_name = 'eyjnhdafb fsazdf'
    ,@sql_data_directory = N'D:\SQL\SQL2014\Data'
    ,@sql_log_directory = N'D:\SQL\SQL2014\Log'
    ,@username = 'webuser'
    ,@ringtail_app_version = null
    ,@debug = 9

*/
