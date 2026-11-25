FOOD ORDERING SYSTEM - AMIT2014 Assignment
============================================
提交前一定要做的最后一步（非常重要！）
============================================
这份代码目前还没有 EF Core Migrations 资料夹（Migrations/）。
在交给老师之前，你们其中一个人（用最后确认能跑起来的那台电脑）
一定要做以下这一步，把 Migrations 资料夹补进最终要交的 zip 里：

1. 打开这个专案（双击 FoodOrderingSystem.sln）
2. Solution Explorer 右键项目 -> Restore NuGet Packages
3. Build -> Rebuild Solution，确认 0 failed
4. 打开 Package Manager Console（Tools > NuGet Package Manager > Package Manager Console）
5. 输入： Add-Migration FinalSubmission
6. 输入： Update-Database
7. 确认按 F5 能正常跑起来，能看到网站、能登入、能下单
8. 这时候项目资料夹里会多出一个 Migrations/ 资料夹 -- 把整个专案资料夹
   （包含这个新的 Migrations 资料夹）重新压缩成 zip，这份才是真正要交给老师的版本

原因：老师打开专案时，如果专案里已经有 Migrations 资料夹，
db.Database.Migrate() 会直接照着 migration 记录建表，不会报错。
如果 Migrations 资料夹是空的（像现在这样），第一次跑起来的人
就要自己跑一次 Add-Migration，很容易在这一步卡住 -- 你们自己都卡过
好几次，老师不见得会想自己 debug。

============================================
提交前的最终检查清单
============================================
[ ] 专案资料夹里有 FoodOrderingSystem.sln（用这个打开，不要直接开 .csproj）
[ ] 专案资料夹里有 Migrations/ 资料夹，而且不是空的
[ ] 找一台"干净"的电脑（例如同学的电脑，没碰过这个项目的）,
    完全照着下面「如何运行」的步骤跑一次，确认能正常打开网站
[ ] 录一段 2-3 分钟的 demo 影片或者截图存起来，
    万一 demo 当下真的环境出问题，至少还有东西可以给老师看
[ ] appsettings.json 里的连接字符串确认是 (localdb)\MSSQLLocalDB
    （比 SQLEXPRESS 更常见，大部分装了 Visual Studio 的电脑都有）

============================================
TECH STACK
============================================
- ASP.NET Core MVC (.NET 10), C#
- Entity Framework Core (code-first, data annotations only)
- SQL Server LocalDB
- 手动 Cookie-based authentication（不是 ASP.NET Core Identity）
- Bootstrap 5 + Chart.js + jQuery Unobtrusive Validation

============================================
HOW TO RUN (Visual Studio 2026)
============================================
1. 双击 FoodOrderingSystem.sln 打开（不要直接开 .csproj）
2. 如果没有自动 restore，右键项目 > Restore NuGet Packages
3. Build > Rebuild Solution，确认 Output 窗口显示 0 failed
4. 确认电脑上有 SQL Server LocalDB（通常跟着 Visual Studio 的
   "ASP.NET and web development" 工作负载一起装好）。
   可以在终端机输入 sqllocaldb info 确认。
5. 按 F5，等终端出现 "Now listening on: http://localhost:5000"，
   如果没有自动开浏览器，手动输入网址打开
6. 首次运行会自动建立数据库并帮你塞几笔示范数据（菜单、账号、优惠码等）

预设的示范账号：

    角色       帐号          密码
    --------   ----------   -----------
    Admin      admin        Admin@123
    Staff      staff1       Staff@123
    Customer   customer1    Cust@123

============================================
如果卡住了，对照下面这张表
============================================

错误信息包含 "NETSDK1004" / "Assets file ... not found"
  -> 没做 NuGet restore。右键项目 > Restore NuGet Packages，
     或在终端输入 dotnet restore

错误信息包含 "PendingModelChangesWarning" / "has pending changes"
  -> 项目里没有 Migrations 资料夹，或者跟目前的 Model 对不上。
     打开 Package Manager Console，输入：
     Add-Migration Fix
     Update-Database

错误信息包含 "network-related or instance-specific error" /
"error: 26 - Error Locating Server/Instance Specified"
  -> SQL Server 连不上。确认 appsettings.json 里写的是
     (localdb)\MSSQLLocalDB，且电脑上有装 LocalDB
     (终端输入 sqllocaldb info 确认)

错误信息包含 "Database 'FoodOrderingSystemDB' already exists"
  -> 之前跑一半留下的旧数据库卡住了。在终端依序输入：
     sqllocaldb stop MSSQLLocalDB
     sqllocaldb delete MSSQLLocalDB
     sqllocaldb create MSSQLLocalDB
     然后重新 F5

错误信息包含 "Access is denied" / "CREATE FILE encountered
operating system error 5"
  -> 数据库文件路径设置有问题（这个已经在 Program.cs 里修好了，
     正常不会再出现；如果出现，确认 Program.cs 最上面有这一行：
     AppDomain.CurrentDomain.SetData("DataDirectory", ...) ）

Visual Studio 弹窗 "Unable to start program ... .exe" /
"The system cannot find the file specified"
  -> 这是上面几个错误连锁反应的结果（build 没成功导致 exe
     没生成）。往上找 Output 窗口（下拉选 Build）里的真正报错，
     照上面对应的项目处理

============================================
项目 / 模块结构（5 人分工）
============================================
1. Security & Admin Maintenance    - AccountController, AdminController
2. Menu Maintenance                - MenuController (含多图上传、自定义分类)
3. Table & Reservation Maintenance - ReservationController
4. Ordering & Cart / My Order      - OrderController, ProfileController, RewardController
5. Kitchen & Fulfillment + Reports - KitchenController

已实现的功能包含（不限于）：角色权限管理、登入锁定、
会员积分与等级、优惠券折扣、多种付款方式、Reorder、
五星评价、心愿单、AJAX 搜索筛选排序分页、多张菜品照片、
Admin 数据图表分析、Top Selling 排行榜等，细节可参考
各 Controller 顶部的模块注释。
