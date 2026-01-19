#!/bin/bash

# Self-signed SSL sertifikat yaratma skripti

# Rənglər
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}=== Self-Signed SSL Sertifikat Yaradılması ===${NC}"
echo ""

# SSL qovluğu yarat
mkdir -p ssl

# Self-signed sertifikat yarat
echo -e "${YELLOW}SSL sertifikat yaradılır...${NC}"
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ssl/key.pem \
    -out ssl/cert.pem \
    -subj "/C=AZ/ST=Baku/L=Baku/O=Company/OU=IT/CN=localhost"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ SSL sertifikat uğurla yaradıldı${NC}"
    echo -e "${YELLOW}  Yer: ./ssl/${NC}"
    echo -e "${YELLOW}  Sertifikat: cert.pem${NC}"
    echo -e "${YELLOW}  Key: key.pem${NC}"
else
    echo -e "${RED}✗ Xəta baş verdi${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}=== Docker Container-lər Başladılır ===${NC}"
docker-compose up -d

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✓ Sistem uğurla başladıldı!${NC}"
    echo ""
    echo -e "${GREEN}Daxil olun: https://localhost${NC}"
    echo ""
    echo -e "${YELLOW}Qeyd: Brauzer təhlükəsizlik xəbərdarlığı verəcək.${NC}"
    echo -e "${YELLOW}Bunun səbəbi self-signed sertifikatdır.${NC}"
    echo -e "${YELLOW}'Advanced' > 'Proceed to localhost' seçin.${NC}"
else
    echo -e "${RED}✗ Docker başladıla bilmədi${NC}"
    exit 1
fi