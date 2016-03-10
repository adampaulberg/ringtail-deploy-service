if object_id('dbo.rp_case_upgrade') is not null
begin
    drop procedure dbo.rp_case_upgrade
end
go

create procedure dbo.rp_case_upgrade
(
     @database_name nvarchar(128)               -- [Required] If the database doesn't exist then we'll call rp_case_upgrade.
    ,@username nvarchar(128)                    -- [Required] Used by rp_case_create if the case doesn't exist.
    ,@ringtail_app_version varchar(25) = null   -- [Optional] If null then we'll use the most recent SQL Components
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

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] START'

declare
     @return int = 0
    ,@sql_component_path nvarchar(2000)
    ,@sql nvarchar(max)

set @ringtail_app_version = nullif(@ringtail_app_version, '')

--====================================================================================================

/* Don't try to run this on a CE or DW database */

if @database_name like '%[_]CE' or @database_name like '%[_]DW'
begin
    set @return = -1
    raiserror('Database [%s] can not be upgraded directly. Upgrade the Case instead.', 16, 1, @database_name)
    return @return
end

--====================================================================================================

/* Validate the @ringtail_app_version */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] Running rp_get_sql_components.'

exec master.dbo.rp_get_sql_components
     @sql_component_path = @sql_component_path output
    ,@ringtail_app_version = @ringtail_app_version output
    ,@debug = @debug

if @sql_component_path is null
begin
    raiserror('Could not find a valid SQL Component path.', 16, 1)
    return @return
end

--====================================================================================================

/* If the database doesn't exist we will create it */

if not exists (select 1 from sys.databases where name = @database_name)
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] [' + @database_name + '] does not exist. Calling rp_case_upgrade.'

    exec master.dbo.rp_case_create
         @database_name = @database_name
        ,@username = @username
        ,@ringtail_app_version = @ringtail_app_version
        ,@debug = @debug

    return @return
end

--====================================================================================================

/* Install the Bootstrap procedures */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] Running rp_install_bootstrap on database [' + @database_name + ']'

exec master.dbo.rp_install_bootstrap
     @database_name = @database_name
    ,@ringtail_app_version = @ringtail_app_version
    ,@sql_component_path = @sql_component_path
    ,@debug = @debug

--====================================================================================================

/* Install Ringtail scripts */

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] Install Ringtail scripts on database [' + @database_name + ']'

/* This section comes from the top of the DatabaseBootstrap.sql file, "Usage example for database upgrade.", in case you need to update to a newer version. */
set @sql = N'
use [<<@database_name>>];

select ''<<@database_name>> before'' as [before upgrade], * from dbo.list_variables where thelabel in (''dbModel'',''dbScriptModel'',''productionModel'',''RingtailApplicationVersion'')

-- resolve database type
declare @dbType varchar(40), @path varchar(400), @reference xml, @ringtailModel varchar(255)
set @ringtailModel = ''<<@ringtail_app_version>>''
if object_id(''dbo.list_variables'',''u'') is not null select @dbType = left(theValue,charindex('' '',theValue)) from dbo.list_variables where theLabel = ''dbModel''
select @dbType = case left(@dbType,2) when ''ca'' then ''case'' when ''po'' then ''portal'' when ''rs'' then ''temp'' when ''rp'' then ''rpf'' end
if @dbType is null raiserror(''Database type unknown.'',11,1)

-- load input parameter and run
select @path = [path] from rs_tempdb.dbo.sqlcomponent_path where version = @ringtailModel
set @reference = ''<reference><targetpath>'' + @path + ''Scripts\'' + @dbType + ''</targetpath></reference>''

exec dbo.rs_sp_database__upgrade @reference

select ''<<@database_name>> after'' as [after upgrade], * from dbo.list_variables where thelabel in (''dbModel'',''dbScriptModel'',''productionModel'',''RingtailApplicationVersion'')

if exists (select 1 from sys.tables where name = ''upgrade'')
begin
    if exists (select 1 from dbo.upgrade where steplabel in (''fatal'', ''error''))
    begin
        select ''<<@database_name>>'' as ''<<@database_name>>'', * from dbo.upgrade where steplabel in (''fatal'', ''error'')
    end
end;
'

select
     @sql = replace(@sql, '<<@database_name>>', @database_name)
    ,@sql = replace(@sql, '<<@ringtail_app_version>>', @ringtail_app_version)

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] @sql: ' + isnull(@sql, '{null}')

if @debug <> 255
    exec sp_executesql @sql

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_case_upgrade] END'

return @return

go

grant exec on dbo.rp_case_upgrade to public
go

/* DEV TESTING

exec master.dbo.rp_case_upgrade
     @database_name = 'Case01'
    ,@username = 'webuser'
    ,@ringtail_app_version = '8.5.001.54' -- exec xp_cmdshell 'dir "C:\Program Files\Ringtail"'
    ,@debug = 9

exec master.dbo.rp_case_upgrade
     @database_name = 'Case01'
    ,@username = 'webuser'
    ,@debug = 9

*/
