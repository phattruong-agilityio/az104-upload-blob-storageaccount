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
    "ConnectionStrings": {
        "AzureBlobStorage": "DefaultEndpointsProtocol=https;AccountName=AccountName;AccountKey=AccountKey;EndpointSuffix=core.windows.net"
    },
    "AzureBlob": {
        "ContainerName": "ContainerName"
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
