Steps to reproduce:
1. Set s certificate /mnt/public.crt as trusted

2. Execute next command: 

docker-compose -f docker-compose.yml up -d  
dotnet test SSEMultipartUpload/SSEMultipartUpload.sln 