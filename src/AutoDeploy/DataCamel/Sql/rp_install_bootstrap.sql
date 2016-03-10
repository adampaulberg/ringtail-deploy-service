if object_id('dbo.rp_install_bootstrap') is not null
begin
    drop procedure dbo.rp_install_bootstrap
end
go

create procedure dbo.rp_install_bootstrap
(
     @database_name nvarchar(128)               -- [Required] Case/Portal/RPF database name.
    ,@ringtail_app_version varchar(25) = null   -- [Optional/Required] The SQL Components version number. Supply @ringtail_app_version or @sql_component_path.
    ,@sql_component_path nvarchar(2000) = null  -- [Optional/Required] The full path to the Scripts folder. Supply @ringtail_app_version or @sql_component_path
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

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] START'

declare
     @return int = 0
    ,@xp_cmdshell varchar(2000)
    ,@sql nvarchar(max)
    ,@server_name nvarchar(128) = @@servername

-- Handle zero-length (empty) strings
select
     @sql_component_path = nullif(@sql_component_path, '')
    ,@ringtail_app_version = nullif(@ringtail_app_version, '')

if @debug >= 3 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] @sql_component_path: ' + isnull(@sql_component_path, '{null}')
if @debug >= 3 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] @ringtail_app_version: ' + isnull(@ringtail_app_version, '{null}')

--====================================================================================================

if not exists (select 1 from sys.databases where name = @database_name)
begin
    set @return = -1
    raiserror('Database [%s] does not exist.', 16, 1, @database_name)
    return @return
end

--====================================================================================================

if @sql_component_path is null and @ringtail_app_version is null
begin
    set @return = -1
    raiserror('Please supply @ringtail_app_version or @sql_component_path.', 16, 1)
    return @return
end

if @sql_component_path is null and @ringtail_app_version is not null
begin
    select @sql_component_path = [path] from rs_tempdb.dbo.SQLComponent_Path where valid = 1 and [version] = @ringtail_app_version

    if @sql_component_path is /* STILL */ null
    begin
        -- This section won't be used much. It would only happen if you are going to try to install SQL Components that weren't installed or got corrupted in the SQL Component table somehow.
        exec master.dbo.rp_get_sql_components
             @sql_component_path = @sql_component_path output
            ,@ringtail_app_version = @ringtail_app_version
            ,@bypass_table = 1
            ,@debug = @debug
    end
end

--====================================================================================================

/* Validate the SQL Component path */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] Running rp_validate_path to verify path to "' + @sql_component_path + '"'

exec master.dbo.rp_validate_path
     @path = @sql_component_path
    ,@debug = @debug

-- Make sure @sql_component_path has an ending slash
select @sql_component_path = master.dbo.rf_directory_slash(null, @sql_component_path, '\')

--====================================================================================================

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] Run DatabaseBootstrap.sql'

select
     @xp_cmdshell = 'sqlcmd -E -S "<<@server_name>>" -d "<<@database_name>>" -I -i "<<@sql_component_path>>Scripts\DatabaseBootstrap.sql"'
    ,@xp_cmdshell = replace(@xp_cmdshell, '<<@server_name>>', @server_name)
    ,@xp_cmdshell = replace(@xp_cmdshell, '<<@database_name>>', @database_name)
    ,@xp_cmdshell = replace(@xp_cmdshell, '<<@sql_component_path>>', @sql_component_path)

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] @xp_cmdshell: ' + isnull(@xp_cmdshell, '{null}')

if @debug >= 5
    exec xp_cmdshell @xp_cmdshell
else
    exec xp_cmdshell @xp_cmdshell, 'no_output'

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_install_bootstrap] END'

return @return

go

grant exec on dbo.rp_install_bootstrap to public
go

/* DEV TESTING

exec master.dbo.rp_install_bootstrap
     @database_name = 'Case01'
    ,@ringtail_app_version = null
    ,@sql_component_path = null
    ,@debug = 1

*/

