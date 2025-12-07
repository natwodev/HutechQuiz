# Hướng dẫn chia sẻ Docker Image

## Cách 1: Push lên Docker Hub (Khuyến nghị)

### Bước 1: Đăng nhập Docker Hub
```bash
docker login
# Nhập username và password Docker Hub của bạn
```

### Bước 2: Tag image với username Docker Hub
```bash
docker tag hutechquiz-app YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
# Ví dụ: docker tag hutechquiz-app nguyenhuynhnam/hutechquiz-app:latest
```

### Bước 3: Push image lên Docker Hub
```bash
docker push YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
```

### Bước 4: Người khác pull và chạy
```bash
# Pull image
docker pull YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest

# Chạy container
docker run -d -p 8080:5163 \
  --name backend_manage \
  YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
```

---

## Cách 2: Export/Import Image thành file .tar

### Export image (người tạo image):
```bash
# Export image thành file tar
docker save -o hutechquiz-app.tar hutechquiz-app:latest

# Nén file để giảm kích thước (tùy chọn)
gzip hutechquiz-app.tar
# Kết quả: hutechquiz-app.tar.gz
```

### Import image (người nhận):
```bash
# Nếu file đã nén, giải nén trước
gunzip hutechquiz-app.tar.gz

# Import image từ file tar
docker load -i hutechquiz-app.tar

# Chạy container
docker run -d -p 8080:5163 --name backend_manage hutechquiz-app:latest
```

---

## Cách 3: Chia sẻ Dockerfile và build lại

### Bước 1: Chia sẻ các file sau:
- `backend_manage/Dockerfile`
- `compose.yaml`
- Source code (hoặc chỉ Dockerfile nếu đã có image)

### Bước 2: Người nhận build lại:
```bash
# Build image với connection string
docker build -t hutechquiz-app \
  --build-arg ConnectionStrings__DefaultConnection='Server=dacn.c5m28sck2nkm.ap-southeast-2.rds.amazonaws.com,1433;Database=hutech_quiz_change_db;User Id=admin;Password=YourStrong!Passw0rd;TrustServerCertificate=True;' \
  -f backend_manage/Dockerfile .

# Chạy với docker-compose
docker-compose up -d
```

---

## Cách 4: Push lên Private Registry (AWS ECR, Azure Container Registry, etc.)

### Ví dụ với AWS ECR:
```bash
# Login vào ECR
aws ecr get-login-password --region ap-southeast-2 | docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.ap-southeast-2.amazonaws.com

# Tag image
docker tag hutechquiz-app:latest YOUR_ACCOUNT_ID.dkr.ecr.ap-southeast-2.amazonaws.com/hutechquiz-app:latest

# Push
docker push YOUR_ACCOUNT_ID.dkr.ecr.ap-southeast-2.amazonaws.com/hutechquiz-app:latest
```

---

## Lưu ý quan trọng:

⚠️ **Bảo mật:**
- Connection string đã được build vào image, nên cẩn thận khi chia sẻ
- Nên sử dụng environment variables thay vì build-arg cho production
- Cân nhắc sử dụng Docker secrets hoặc external config

✅ **Khuyến nghị:**
- Sử dụng Docker Hub cho public hoặc private repository
- Hoặc AWS ECR/Azure ACR cho enterprise
- Export/Import chỉ phù hợp cho chia sẻ nội bộ

