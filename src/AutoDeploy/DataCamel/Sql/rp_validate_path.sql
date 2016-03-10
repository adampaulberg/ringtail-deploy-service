if object_id('dbo.rp_validate_path') is not null
begin
    drop procedure dbo.rp_validate_path
end
go

create procedure dbo.rp_validate_path
(
     @path nvarchar(4000)
    ,@debug tinyint = 0
)
with encryption
as

set nocount on
--set xact_abort on -- disabled on purpose.
set transaction isolation level read uncommitted

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_validate_path] START'

/* Suggested @debug values
1 = Simple print statements
2 = Simple select statements (e.g. select @variable_1 as variable_1, @variable_2 as variable_2)
3 = Result sets from temp tables (e.g. select '#temp_table_name' as '#temp_table_name' from #temp_table_name where ...)
4 = @sql statements from exec() or sp_executesql
*/

declare
     @return int = 0
    ,@xp_cmdshell varchar(2000)

declare @output table
(
     ident int not null identity(1,1) primary key clustered
    ,value nvarchar(max) null
)

--====================================================================================================

select
     @xp_cmdshell = 'dir "<<@path>>"'
    ,@xp_cmdshell = replace(@xp_cmdshell, '<<@path>>', @path)

if @debug >= 4 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_validate_path] @xp_cmdshell: ' + isnull(@xp_cmdshell, N'{null}')

insert @output (value)
    exec xp_cmdshell @xp_cmdshell

if @debug >= 4 select '@output' as 'rp_validate_path @output', * from @output

if exists (select 1 from @output where value in ('The system cannot find the path specified.', 'File Not found'))
begin
    set @return = -1
    raiserror('The system cannot find the path specified. Is "%s" correct?', 16, 1, @path)
    return @return
end

if @debug >= 1 print '[' + convert(varchar(23), getdate(), 121) + '] [rp_validate_path] END'

return @return

go

grant exec on dbo.rp_validate_path to public
go

/* DEV TESTING

declare @return int = 0

exec @return = master.dbo.rp_validate_path
     @path = 'C:\Program Files\Ringtail\SQL Component_v8.5.000.143xxxxxxxx'
    ,@debug = 0

print @return

*/

