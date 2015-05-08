-- Written by Greg (Lord Duffcakes) Duffie
--

--C# will automatically connect to master, no need for this
--use master
--go

if object_id('dbo.rp_case_backup') is not null
begin
    drop procedure dbo.rp_case_backup
end
go

create procedure dbo.rp_case_backup
(
     @database_name nvarchar(128)       -- [Required] Actual database name that you are backing up (e.g., Longford).
    ,@full_path nvarchar(4000)  = null  -- [Optional] The full path to the .bak file (e.g., D:\SQL\Backups\Longford.bak). If not supplied we will use the default.
    ,@overwrite bit = 0                 -- [Optional] If a backup exists with the same name, Overwrite = 0 will append this backup to that one otherwise it will overwrite it.
    ,@name nvarchar(128) = null         -- [Optional] Name of the backup set. This is what you see in the SSMS UI.
    ,@description nvarchar(255) = null  -- [Optional] Describes the backup set. This is NOT seen in the SSMS UI.
    ,@debug tinyint = 0
)
with encryption
as

set nocount on
set xact_abort on
set transaction isolation level read uncommitted

/* Suggested @debug values
1 = Simple print statements
2 = Simple select statements (e.g. select @variable_1 as variable_1, @variable_2 as variable_2)
3 = Result sets from temp tables (e.g. select '#temp_table_name' as '#temp_table_name' from #temp_table_name where ...)
4 = @sql statements from exec() or sp_executesql
*/

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] START'

declare
     @return int = 0
    ,@sql nvarchar(1000)
    ,@sql_params nvarchar(100)
    ,@ringtail_app_version nvarchar(25)
    ,@ringtail_db_version nvarchar(25)
    ,@directory nvarchar(4000) -- Directory only

--====================================================================================================

if not exists (select 1 from sys.databases where name = @database_name)
begin
    set @return = -1
    raiserror('Database [%s] does not exist on this server.', 16, 1, @database_name)
    return @return
end

--====================================================================================================

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] Getting app and db version numbers on database [' + @database_name + ']'

select
     @sql_params = N'@ringtail_app_version nvarchar(25) output, @ringtail_db_version nvarchar(25) output'
    ,@sql = N'
    use [<<@database_name>>]
    if exists (select 1 from dbo.list_variables)
    begin
        select @ringtail_app_version = ltrim(rtrim(thevalue)) from dbo.list_variables where thelabel = ''RingtailApplicationVersion'';
        select @ringtail_db_version = ltrim(rtrim(thevalue)) from dbo.list_variables where thelabel = ''RingtailDatabaseModel'';
    end
    '
    ,@sql = replace(@sql, '<<@database_name>>', @database_name)

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @sql: ' + isnull(@sql, '{null}')

exec sp_executesql
     @sql, @sql_params
    ,@ringtail_app_version = @ringtail_app_version output
    ,@ringtail_db_version = @ringtail_db_version output

if @debug >= 2
begin
    print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @ringtail_app_version: ' + isnull(@ringtail_app_version, '{null}')
    print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @ringtail_db_version: ' + isnull(@ringtail_db_version, '{null}')
end

--====================================================================================================

-- Create strings

select
     @name = case when datalength(@name) > 0 then @name + N' - ' else N'' end
    ,@name = @name + N'App (<<@ringtail_app_version>>); DB (<<@ringtail_db_version>>)'
    ,@name = replace(@name, '<<@ringtail_app_version>>', @ringtail_app_version)
    ,@name = replace(@name, '<<@ringtail_db_version>>', @ringtail_db_version)

    ,@description = case when datalength(@description) > 0 then @description + N' - ' else N'' end
    ,@description = @description + N'Date (<<@date>>); Server (<<@server>>)'
    ,@description = replace(@description, '<<@date>>', convert(varchar(23), getdate(), 121))
    ,@description = replace(@description, '<<@server>>', @@servername)

if @debug >= 2
begin
    print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @name: ' + isnull(@name, '{null}')
    print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @description: ' + isnull(@description, '{null}')
end

--====================================================================================================

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] Validating backup path'

if nullif(@full_path, '') is null -- The .bak name wasn't supplied either
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @full_path was empty. Checking registry for default location.'

    exec master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'BackupDirectory', @full_path output, 'no_output'

    set @directory = @full_path

    -- Now add @database_name and .bak to the @full_path
    set @full_path = @full_path + N'\' + @database_name + N'.bak'
end
else
begin
    -- Remove the database name and extension (since they won't exist on the first backup) and just validate the directory. If @full_path wasn't supplied then this step isn't necessary.
    set @directory = substring(@full_path, 1, len(@full_path) - charindex('\', reverse(@full_path)))
end

exec @return = master.dbo.rp_validate_path
     @path = @directory
    ,@debug = @debug

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] Backing up database [' + @database_name + '] to ' + @full_path

--====================================================================================================

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] Backing up database [' + @database_name + ']'

select
     @sql = 'BACKUP DATABASE [<<@database_name>>] TO DISK = N''<<@full_path>>'' WITH DESCRIPTION = N''<<@description>>'', NOFORMAT, <<@init_noinit>>,  NAME = N''<<@name>>'', SKIP, NOREWIND, NOUNLOAD, STATS = 10'
    ,@sql = replace(@sql, '<<@database_name>>', @database_name)
    ,@sql = replace(@sql, '<<@full_path>>', @full_path)
    ,@sql = replace(@sql, '<<@description>>', @description)
    ,@sql = replace(@sql, '<<@init_noinit>>', case @overwrite when 1 then 'INIT' else 'NOINIT' end)
    ,@sql = replace(@sql, '<<@name>>', @name)

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] @sql: ' + isnull(@sql, '{null}')

exec sp_executesql @sql

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_backup] END'

return @return

go

grant exec on dbo.rp_case_backup to public
go

/* DEV TESTING

-- Backup Longford to "foo.bak"
exec master.dbo.rp_case_backup
     @database_name = 'Longford'
    ,@full_path = 'D:\SQL\Backup\foo.bak'
    ,@overwrite = 0
    ,@name = N'Longford'
    ,@description = N'I said, Longford!'
    ,@debug = 3

-- Restore "foo.bak" as "asfubjghhaifuvbh"
exec master.dbo.rp_case_restore
     @database_name = 'asfubjghhaifuvbh'
    ,@full_path = 'D:\SQL\Backup\foo.bak'
    ,@debug = 3

-- Drop "asfubjghhaifuvbh"
exec master.dbo.rp_case_drop
     @database_name = 'asfubjghhaifuvbh'
    ,@debug = 3

*/
