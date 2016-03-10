if object_id('dbo.rp_get_sql_components') is not null
begin
    drop procedure dbo.rp_get_sql_components
end
go

create procedure dbo.rp_get_sql_components
(
     @sql_component_path nvarchar(2000) = null output   -- [Output] The full path to the Scripts folder.
    ,@ringtail_app_version varchar(25) = null output    -- [Output] The SQL Component version number.
    ,@bypass_table bit = 0                              -- [Optional] Bypass the SQLComponent_Path table and just use the most recent install on the file system.
    ,@debug tinyint = 0
)
with encryption
as

set nocount on
--set xact_abort on -- disabled on purpose.
set transaction isolation level read uncommitted

/* Suggested @debug values
1 = Simple print statements
2 = Simple select statements (e.g. select @variable_1 as variable_1, @variable_2 as variable_2)
3 = Result sets from temp tables (e.g. select '#temp_table_name' as '#temp_table_name', * from #temp_table_name where ...)
4 = @sql statements from exec() or sp_executesql
*/

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_get_sql_components] START'

declare
     @return int = 0
    ,@xp_cmdshell varchar(2000)
    ,@ringtail_path nvarchar(2000) = 'C:\Program Files\Ringtail'

declare @output table
(
     ident int not null identity(1,1) primary key clustered
    ,value nvarchar(max) null
)

if @bypass_table = 0
begin
    /* Look in the rs_tempdb.dbo.SQLComponent_Path table for the most recent valid row first */

    select top 1
         @sql_component_path = [path]
        ,@ringtail_app_version = [version]
    from
        rs_tempdb.dbo.SQLComponent_Path
    where
        valid = 1
    order by
        id desc

    exec master.dbo.rp_validate_path
         @path = @sql_component_path
        ,@debug = @debug

    /* If there are no valid installs, try the most recent "invalid" install */

    if @sql_component_path is null or @return <> 0
    begin
        select top 1
             @sql_component_path = [path]
            ,@ringtail_app_version = [version]
        from
            rs_tempdb.dbo.SQLComponent_Path
        order by
            id desc

        exec master.dbo.rp_validate_path
             @path = @sql_component_path
            ,@debug = @debug
    end
end

/* If the SQLComponent_Path isn't working see if there's anything on the file system
Q. Why would this ever happen?
A. Sometimes I create a fake copy of the components for testing and I don't put it in the SQLComponent_Path table.
*/

if @bypass_table = 1 or @sql_component_path is null or @return <> 0
begin
    if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_get_sql_components] Running rp_validate_path to check for the "' + @ringtail_path + '" folder'

    -- Make sure the Ringtail folder exists
    exec master.dbo.rp_validate_path
         @path = @ringtail_path
        ,@debug = @debug

    if @return = 0
    begin
        if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_get_sql_components] Finding the latest SQL Components'
    
        set @xp_cmdshell = 'dir "C:\Program Files\Ringtail\" /O:-D'

        if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_get_sql_components] @xp_cmdshell: ' + isnull(@xp_cmdshell, N'{null}')

        insert @output (value)
            exec xp_cmdshell @xp_cmdshell

        if @debug >= 4 select '@output' as 'rp_get_sql_components @output', * from @output

        select top 1
            @ringtail_app_version = substring(value, charindex('SQL Component_v', value) + 15, 25)
        from
            @output
        where
            value like '%SQL Component[_]v%'

        set @sql_component_path = @ringtail_path + N'\SQL Component_v' + @ringtail_app_version

        -- Make sure the SQL Component path exists
        exec master.dbo.rp_validate_path
             @path = @sql_component_path
            ,@debug = @debug
    end
end

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_get_sql_components] END'

return @return

go

grant exec on dbo.rp_get_sql_components to public
go

/* DEV TESTING

declare
     @return int = 0
    ,@sql_component_path nvarchar(2000)
    ,@ringtail_app_version varchar(25)
     
exec master.dbo.rp_get_sql_components
     @sql_component_path = @sql_component_path output
    ,@ringtail_app_version = @ringtail_app_version output
    ,@bypass_table = 0
    ,@debug = 0

select
     @sql_component_path as sql_component_path
    ,@ringtail_app_version as ringtail_app_version
    ,@return as [return]

exec master.dbo.rp_get_sql_components
     @sql_component_path = @sql_component_path output
    ,@ringtail_app_version = @ringtail_app_version output
    ,@bypass_table = 1
    ,@debug = 0

select
     @sql_component_path as sql_component_path
    ,@ringtail_app_version as ringtail_app_version
    ,@return as [return]

*/
