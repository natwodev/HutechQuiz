# Hướng dẫn chạy Docker Image trên Ubuntu

## Bước 1: Push Image lên Docker Hub (Trên máy hiện tại - macOS)

### 1.1. Đăng nhập Docker Hub
```bash
docker login
# Nhập username và password Docker Hub của bạn
```

### 1.2. Tag image với username Docker Hub
```bash
docker tag hutechquiz-app:latest YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
# Ví dụ: docker tag hutechquiz-app:latest nguyenhuynhnam/hutechquiz-app:latest
```

### 1.3. Push image lên Docker Hub
```bash
docker push YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
```

---

## Bước 2: Trên máy Ubuntu

### 2.1. Cài đặt Docker (nếu chưa có)
```bash
# Update package index
sudo apt-get update

# Install Docker
sudo apt-get install -y docker.io docker-compose

# Start Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Thêm user vào docker group (để không cần sudo)
sudo usermod -aG docker $USER
# Logout và login lại để áp dụng
```

### 2.2. Đăng nhập Docker Hub
```bash
docker login
# Nhập cùng username và password Docker Hub
```

### 2.3. Pull image về
```bash
docker pull YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
```

### 2.4. Tạo file docker-compose.yaml trên Ubuntu

Tạo file `compose.yaml` với nội dung:

```yaml
services:
  backend_manage:
    image: YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
    ports:
      - "8080:5163"
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    volumes:
      - ./backend_manage/Logs:/app/Logs
    restart: unless-stopped
```

### 2.5. Chạy container
```bash
# Tạo thư mục logs (nếu cần)
mkdir -p ./backend_manage/Logs

# Chạy container
docker-compose up -d

# Xem logs
docker-compose logs -f backend_manage

# Kiểm tra trạng thái
docker-compose ps
```

### 2.6. Kiểm tra API
```bash
# Test API
curl http://localhost:8080/api/Lecturer/reset-exam-session-start-time

# Hoặc từ máy khác
curl http://UBUNTU_IP:8080/api/Lecturer/reset-exam-session-start-time
```

---

## Hoặc chạy trực tiếp với docker run:

```bash
docker run -d \
  --name backend_manage \
  -p 8080:5163 \
  -p 8081:8081 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  -v $(pwd)/backend_manage/Logs:/app/Logs \
  --restart unless-stopped \
  YOUR_DOCKERHUB_USERNAME/hutechquiz-app:latest
```

---

## Lưu ý:

1. **Firewall**: Đảm bảo port 8080, 8081 đã được mở trên Ubuntu
   ```bash
   sudo ufw allow 8080/tcp
   sudo ufw allow 8081/tcp
   ```

2. **Kiểm tra image đã pull về:**
   ```bash
   docker images | grep hutechquiz-app
   ```

3. **Xem logs real-time:**
   ```bash
   docker logs -f backend_manage
   ```

4. **Stop/Start container:**
   ```bash
   docker-compose stop
   docker-compose start
   # hoặc
   docker stop backend_manage
   docker start backend_manage
   ```

