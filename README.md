Below is an updated `README.md` for your `TestingIV` project, tailored to reflect your specific `Dockerfile` and current setup. This version is designed for your GitHub repository, providing clear instructions and details for users.

---

# TestingIV

![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Docker](https://img.shields.io/badge/Docker-Containerized-blue)
![License](https://img.shields.io/badge/License-MIT-green)

**TestingIV** is an ASP.NET Core Web API that processes transaction requests, applies discount rules, and logs activities for troubleshooting. It features robust validation, signature verification, and discount calculations, all containerized using Docker for portability and consistency.

## Features
- **Transaction Processing**: Handles requests with fields like `PartnerKey`, `PartnerRefNo`, `TotalAmount`, etc.
- **Validation**: Ensures data integrity with model annotations and custom logic (e.g., timestamp ±5 minutes, signature verification).
- **Discount Rules**: Applies base and conditional discounts with a 20% cap based on `TotalAmount`.
- **Logging**: Uses log4net to save request/response bodies and logs to `Logs/TransactionLog.txt`.
- **Logs Path: Noted the local (bin/Debug/net8.0/Logs) and Docker (/app/Logs) paths.
- **Dockerized**: Packaged in a Docker container running on port 8080.

## Project Structure
```
TestingIV/
├── Controllers/
│   └── TransactionController.cs
├── Models/
│   └── TransactionRequestViewModel.cs
├── Services/
│   ├── IPartnerService.cs
│   ├── PartnerService.cs
│   ├── ISignatureService.cs
│   └── SignatureService.cs
├── Logs/                   # Created at runtime for logs
├── Dockerfile
├── log4net.config
├── Program.cs
├── TestingIV.csproj
└── README.md
```
app/
```
## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started)
- Git (for cloning the repo)

## Getting Started

### Clone the Repository
```bash
git clone https://github.com/hazimmarhaimi/TestingApi.git
cd TestingApi
```

### Build and Run Locally (Without Docker)
1. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```
2. **Build the Project**:
   ```bash
   dotnet build
   ```
3. **Run the Application**:
   ```bash
   dotnet run
   ```
   - The API runs on `http://localhost:5000/api/submittrxmessage` (or `https://localhost:5001` if HTTPS is enabled).
   - Logs are written to `bin/Debug/net8.0/Logs/TransactionLog.txt`.

### Build and Run with Docker
1. **Build the Docker Image**:
   ```bash
   docker build -t testingiv:latest .
   ```
2. **Run the Container**:
   ```bash
   docker run -d -p 8080:8080 --name testingiv-container testingiv:latest
   ```
   - The API runs on `http://localhost:8080/api/submittrxmessage`.
   - Logs are written to `/app/Logs/TransactionLog.txt` inside the container.

3. **Persist Logs (Optional)**:
   Map a host directory to the container’s log path:
   ```bash
   docker run -d -p 8080:8080 -v ./Logs:/app/Logs --name testingiv-container testingiv:latest
   ```
   - Logs will appear in `./Logs/TransactionLog.txt` on your host.

4. **View Logs**:
   ```bash
   docker cp testingiv-container:/app/Logs/TransactionLog.txt ./TransactionLog.txt
   cat TransactionLog.txt
   ```

### Stop and Clean Up
- Stop the container:
  ```bash
  docker stop testingiv-container
  ```
- Remove the container:
  ```bash
  docker rm testingiv-container
  ```

## API Usage

### Endpoint
- **POST** `/api/submittrxmessage`

### Request Body
```json
{
  "PartnerKey": "FAKEGOOGLE",
  "PartnerRefNo": "FG-00001",
  "PartnerPassword": "RkFLRVBBU1NXT1JEMTIzNA==", // Base64 "FAKEPASSWORD1234"
  "TotalAmount": 100000,                           // In cents (MYR 1000)
  "Items": [],
  "Timestamp": "2025-03-18T12:00:00Z",            // ISO 8601 UTC
  "Sig": "AX4NiI06xU0O7fbmbina7ozFdFkj3cp13MmQyGG+ARM=" // SHA256 hash
}
```

### Response (Success)
```json
{
  "result": 1,
  "resultMessage": "Success",
  "totalAmount": 100000,
  "totalDiscount": 10000,  // 10% discount
  "finalAmount": 90000
}
```

### Response (Error)
```json
{
  "result": 0,
  "resultMessage": "Signature mismatch."
}
```

### Signature Calculation
The `Sig` field is a Base64-encoded SHA256 hash of:
```
timestamp (yyyyMMddHHmmss) + PartnerKey + PartnerRefNo + TotalAmount + PartnerPassword
```
Example: `20250318120000FAKEGOOGLEFG-00001100000RkFLRVBBU1NXT1JEMTIzNA==`.

## Discount Rules
- **Base Discount**:
  - < MYR 200: 0%
  - MYR 200–500: 5%
  - MYR 501–800: 7%
  - MYR 801–1200: 10%
  - > MYR 1200: 15%
- **Conditional Discounts**:
  - Prime number > MYR 500: +8%
  - Ends in 5 and > MYR 900: +10%
- **Cap**: Total discount ≤ 20% of `TotalAmount`.

## Logging
- Logs are written to `Logs/TransactionLog.txt`.
  - Locally: `bin/Debug/net8.0/Logs/TransactionLog.txt`.
  - Docker: `/app/Logs/TransactionLog.txt` (mapped to host with `-v` if specified).
- Includes request/response bodies and events.
- Example log entry:
  ```
  2025-03-18 12:00:00,000 [1] INFO  TestingIV.Controllers.TransactionController - Received request: { ... }
  2025-03-18 12:00:00,050 [1] INFO  TestingIV.Controllers.TransactionController - Response sent: { ... }
  ```

## Docker Details
Your `Dockerfile`:
```dockerfile
# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TestingIV.dll"]
```
- **Port**: Runs on 8080 inside the container, mapped to 8080 on the host by default.
- **Environment**: `ASPNETCORE_URLS` ensures the app listens on 8080.

## Dependencies
- **log4net**: For file-based logging.
- **Newtonsoft.Json**: For JSON serialization in logs.

## Troubleshooting
- **API Not Responding**: Check `docker ps` to ensure the container is running. Verify port mapping (`-p 8080:8080`).
- **No Logs**: Ensure `log4net.config` is copied to the output directory and the `Logs` folder exists (created automatically by log4net if the path is writable).
- **Build Fails**: Run `dotnet restore` locally to confirm dependencies.

## Contributing
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature`).
3. Commit changes (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a Pull Request.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.




