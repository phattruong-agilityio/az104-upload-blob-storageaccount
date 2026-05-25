# Azure Blob Storage practice

### Simple practice project for:

* Upload / Get Azure Blob storage
* Connect to Azure Blob Storage using Storage Access keys
* Upload file to Blob Storage

### Requirements

* .NET 10 SDK
* Azure Storage Account
* Storage Account Name
* Storage Account Key

### Setup

- Config Files "appsettings.json" following "appsettings.Examle.json"

    ```json
    "AzureBlob": {
        "ContainerName": "development",
        "ServiceUri": "https://<your-storage-account>.blob.core.windows.net"
    }
    ```

- Restore, build packages

    ```bash
    dotnet restore
    dotnet build
    ```

- Run app

    ```bash
    dotnet run
    ```

- Open web-browser and open "Swagger"

    ```
    http://localhost:5164/swagger/index.html
    ```

## Demo


https://github.com/user-attachments/assets/f58b9e35-3b52-4df9-bea8-5e3878416c15

