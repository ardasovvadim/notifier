# Notifier

Notifier is a .NET Core background service that synchronizes new movie seasons from Rezka.ag and sends email notifications using SendGrid API.

## Prerequisites

- [.NET Core 7 runtime](https://dotnet.microsoft.com/download/dotnet/7.0)
- [MySQL Server](https://www.mysql.com/downloads/)
- [SendGrid API key](https://sendgrid.com/docs/ui/account-and-settings/api-keys/)

## Configuration

- Set the database settings in `appsettings.json`.
- Set the SendGrid settings in `appsettings.json`.
- Set the Rezka.ag settings in `appsettings.json`.

## Usage

- Run the application with `dotnet run`.
- The application will synchronize new movie seasons from Rezka.ag every hour and send email notifications for new seasons using SendGrid API.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.