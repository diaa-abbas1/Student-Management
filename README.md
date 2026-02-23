# Student Management

Simple Windows Forms application (C# .NET Framework 4.7.2) to manage students, courses and grades using SQL Server (local `SQLEXPRESS`).

## Features
- Add / edit / delete students and courses
- Record and list grades with student and course details


## Tech
- C# 7.3, .NET Framework 4.7.2
- Windows Forms (WinForms)
- SQL SERVER DATABASE 
- No external packages required

## Quick setup
1. Clone the repo:
   - `git clone https://github.com/USERNAME/REPO.git`
2. Open the solution in Visual Studio 2022.
3. Update the connection string in `DatabaseHelper.cs` (do not commit credentials):
   - change `connectionString` to match your server / database.
4. Build and run.

Important: keep secret data out of source control. Use environment variables or a local config file for production secrets.
 

## Contributing
Fixes and improvements welcome. Open an issue / PR.

## License
Add your preferred license file (e.g. `MIT`) before publishing.
