@echo off
IF NOT EXIST .git (
echo This is not a git repository. Making it a git repository...
git init
git add .
git commit -m "Initial Commit"
git remote add origin https://github.com/IObsequious/LinqToXsd.git
git remote -v
git push origin master
)

IF EXIST .git (
set /P MESSAGE=Enter a commit message:   
git commit -m "%MESSAGE%"
git push origin master
exit /b
)