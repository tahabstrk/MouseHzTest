# 🖱️ MouseHzTest

Windows Forms uygulaması – farelerin gerçek polling rate değerlerini ölçer.  
Logitech Superlight 2 DEX, Razer 8K Hz gibi yüksek örnekleme hızına sahip mouse’lar için geliştirilmiştir.  

---

## 🚀 Özellikler
- **Instant Hz:** Her olay arasındaki anlık frekans  
- **Average Hz:** Son 500 örneğin ortalaması  
- **Peak Hz:** Oturum boyunca ölçülen en yüksek değer  
- **CSV Kaydı:** Masaüstüne otomatik kayıt

---

## 🧩 Gereksinimler
- Windows 10 / 11  
- .NET 8 Runtime veya SDK  
  👉 [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## 🧱 Derleme (kaynak koddan)
```bash
git clone https://github.com/tahabstrk/MouseHzTest.git
cd MouseHzTest
dotnet build
dotnet run
