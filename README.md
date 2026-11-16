# zhaba228_unik

Університетський проект: **DoctorApp**, вебзастосунок на ASP.NET Core MVC (.NET 8).

## Структура

| Шлях | Що там |
|---|---|
| [`DoctorApp/`](DoctorApp) | код проекту, відкривати `DoctorApp/DoctorApp.slnx` у Visual Studio |
| [`Tasks.md`](Tasks.md) | сюди пишемо завдання |
| [`response.md`](response.md) | тут з'являються відповіді |

## Як запустити

Потрібен [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (або Visual Studio з пакетом "ASP.NET and web development").

```bash
cd DoctorApp
dotnet run
```

Або відкрити `DoctorApp/DoctorApp.slnx` у Visual Studio і натиснути ▶.

## Як ми працюємо

1. Пишеш завдання або питання в [`Tasks.md`](Tasks.md), робиш коміт і пушиш.
2. Відповідь і зміни в коді з'являються після `git pull`, а пояснення до них лежить у [`response.md`](response.md).
3. Повторюємо.

```bash
git pull                       # отримати свіжі зміни
git add .
git commit -m "опис змін"
git push                       # відправити свої зміни
```
