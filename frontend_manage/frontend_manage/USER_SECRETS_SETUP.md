# Hướng dẫn cấu hình User Secrets

## User Secrets là gì?

User Secrets là cách chuẩn của .NET để lưu trữ thông tin nhạy cảm (như API URL) trong quá trình development. Dữ liệu được lưu ngoài project và không bao giờ được commit vào git.

## Cách setup User Secrets

### 1. Khởi tạo User Secrets (chỉ cần làm 1 lần)

```bash
cd frontend_manage/frontend_manage
dotnet user-secrets init
```

### 2. Thêm API Base URL vào User Secrets

```bash
dotnet user-secrets set "ApiBaseUrl" "https://natwo.online/"
```

### 3. Kiểm tra User Secrets đã được lưu

```bash
dotnet user-secrets list
```

### 4. Xóa User Secret (nếu cần)

```bash
dotnet user-secrets remove "ApiBaseUrl"
```

## Lưu ý

- User Secrets chỉ hoạt động trong môi trường Development
- Dữ liệu được lưu tại: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (Windows)
- User Secrets có độ ưu tiên cao hơn `appsettings.json`
- Không bao giờ commit file `secrets.json` vào git (đã được tự động ignore)

## Fallback

Nếu User Secrets không được cấu hình, ứng dụng sẽ đọc từ `appsettings.json`. 
**Lưu ý:** File `appsettings.json` không nên chứa URL thật trong production, nên thêm vào `.gitignore`.

