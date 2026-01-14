# WebRTC Video Konfrans Sistemi (Docker + Self-Signed SSL)

Lokal istifadə üçün self-signed SSL sertifikat ilə WebRTC video konfrans sistemi.

## 📁 Layihə Strukturu

```
webrtc-conference/
├── docker-compose.yml          # Docker Compose konfiqurasiyası
├── init-ssl.sh                 # SSL yaratma və başlatma skripti
├── app/
│   ├── Dockerfile             # WebRTC app Dockerfile
│   ├── package.json           # Node.js dependency-lər
│   ├── server.js              # Backend server
│   └── public/
│       └── index.html         # Frontend interface
├── nginx/
│   ├── nginx.conf             # Əsas Nginx konfiqurasiyası
│   └── conf.d/
│       └── default.conf       # Site konfiqurasiyası
├── ssl/                       # SSL sertifikatlar (avtomatik yaranır)
│   ├── cert.pem              # Sertifikat
│   └── key.pem               # Private key
└── README.md
```

## 🚀 Quraşdırma (3 addım)

### 1. Layihəni hazırlayın

```bash
# Qovluqları yaradın
mkdir -p webrtc-conference/{app/public,nginx/conf.d}
cd webrtc-conference
```

### 2. Faylları yerləşdirin

Bütün artifactları müvafiq qovluqlara kopyalayın:
- `docker-compose.yml` → ana qovluq
- `init-ssl.sh` → ana qovluq
- `app/Dockerfile` → app/
- `app/package.json` → app/
- `app/server.js` → app/
- `app/public/index.html` → app/public/
- `nginx/nginx.conf` → nginx/
- `nginx/conf.d/default.conf` → nginx/conf.d/

### 3. Başlatın!

```bash
# Skripti icra edilə bilən edin
chmod +x init-ssl.sh

# SSL yarat və sistemi başlat
./init-ssl.sh
```

**Bu qədər! ✅**

Brauzerdə açın: `https://localhost`

## 🔐 Mövcud Sertifikatınızı İstifadə Etmək

Əgər artıq sertifikatınız varsa:

```bash
# SSL qovluğu yaradın
mkdir -p ssl

# Sertifikatınızı kopyalayın
cp /yol/sertifikat.pem ssl/cert.pem
cp /yol/key.pem ssl/key.pem

# Docker-u başladın
docker-compose up -d
```

## 🌐 İstifadə

1. Brauzerdə: `https://localhost`
2. Təhlükəsizlik xəbərdarlığı gələcək (self-signed olduğu üçün)
3. **Chrome/Edge:** "Advanced" → "Proceed to localhost"
4. **Firefox:** "Advanced" → "Accept the Risk and Continue"
5. Adınızı və otaq ID daxil edin
6. Başqa cihazlardan da eyni şəbəkədə qoşula bilərsiniz: `https://SERVER-IP`

## 🔧 İdarəetmə Əmrləri

```bash
# Başlat
docker-compose up -d

# Dayandır
docker-compose down

# Restart
docker-compose restart

# Loglar
docker-compose logs -f

# Xüsusi servis logu
docker-compose logs -f webrtc-app
docker-compose logs -f nginx

# Container statusu
docker-compose ps
```

## 🌐 Şəbəkə Girişi

Eyni şəbəkədəki digər cihazlardan girmək üçün:

1. **Server IP-ni tapın:**
```bash
# Linux/Mac
ifconfig | grep "inet "

# Windows
ipconfig
```

2. **Digər cihazdan:**
```
https://192.168.1.X  (server IP)
```

3. **Brauzer xəbərdarlığını qəbul edin**

## ⚙️ Konfiqurasiya

### Port dəyişdirmək

`docker-compose.yml` faylında:
```yaml
nginx:
  ports:
    - "8080:80"    # HTTP
    - "8443:443"   # HTTPS
```

### Server IP ilə işləmək

`nginx/conf.d/default.conf` faylında:
```nginx
server_name localhost 192.168.1.100;  # IP əlavə edin
```

## 🐛 Problem Həlli

### 1. Port məşğuldur

```bash
# Portları yoxlayın
sudo lsof -i :80
sudo lsof -i :443

# Məşğul portları azad edin və ya docker-compose-da dəyişin
```

### 2. Brauzer sertifikatı qəbul etmir

- **Chrome/Edge:** chrome://flags → "Allow invalid certificates for localhost" → Enable
- **Firefox:** Hər dəfə "Accept Risk" deməlisiniz
- Və ya real sertifikat istifadə edin

### 3. Kamera/Mikrofon işləmir

```bash
# HTTPS-dən əmin olun
echo "HTTPS olmadan media cihazlar işləməz"

# Brauzer icazələrini yoxlayın
# Chrome: Settings > Privacy > Site Settings > Camera/Microphone
```

### 4. WebSocket bağlanmır

```bash
# Nginx loglarına baxın
docker-compose logs nginx

# Proxy ayarlarını yoxlayın
docker-compose exec nginx nginx -t
```

## 📊 Texniki Detallar

**Stack:**
- Node.js 18 (Alpine)
- Express.js
- Socket.IO
- Nginx (Alpine)
- WebRTC

**Port-lar:**
- 80 → HTTP (HTTPS-ə redirect)
- 443 → HTTPS
- 3000 → WebRTC App (daxili)

**SSL:**
- Self-signed sertifikat
- RSA 2048-bit
- 365 gün etibarlı
- TLS 1.2, 1.3

## 🔄 Kod Yeniləmək

```bash
# Kod dəyişdirin
nano app/server.js

# Container-i rebuild edin
docker-compose build webrtc-app

# Yenidən başladın
docker-compose up -d
```

## 📝 İstifadəli Əmrlər

```bash
# SSL sertifikatı yenilə (yeni yaratmaq üçün)
rm -rf ssl/
./init-ssl.sh

# Bütün container və volume-ləri sil
docker-compose down -v
docker system prune -a

# Yalnız app-ı restart et
docker-compose restart webrtc-app

# Live kod dəyişikliyi üçün (development)
docker-compose up  # -d olmadan
```

## 🎯 Növbəti Addımlar

Gələcəkdə əlavə edə biləcəyiniz:

1. **Autentifikasiya:**
   - JWT token sistemi
   - User database (PostgreSQL/MongoDB)
   - Login/Register səhifələri

2. **TURN Server:**
   - NAT/Firewall keçmək üçün
   - coturn və ya xirsys

3. **Əlavə Funksiyalar:**
   - Otaq şifrələri
   - Ekran qeydi
   - Virtual background
   - Admin paneli

4. **Monitoring:**
   - Prometheus + Grafana
   - Error tracking
   - Usage statistics

## ⚠️ Vacib Qeydlər

1. **Self-signed sertifikat** production üçün uyğun deyil
2. **Lokal şəbəkədə** işləyəcək (192.168.x.x)
3. **Internet üzərindən** giriş üçün real SSL lazımdır
4. **Mobile cihazlar** xəbərdarlıq verəcək, qəbul etməlisiniz

## 🔒 Təhlükəsizlik

Lokal istifadə üçün təhlükəsizdir, amma:
- Production üçün real SSL istifadə edin
- Autentifikasiya əlavə edin
- Firewall qaydalarını konfiqurasiya edin
- HTTPS məcburi edin

---

**Uğurlar! 🚀**

Suallarınız varsa buyurun!