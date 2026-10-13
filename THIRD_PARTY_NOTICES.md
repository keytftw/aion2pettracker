# Kaynak ve lisanslar

Bu bağımsız Pet Tracker GPL-3.0 altında sunulur; tam karşılık gelen kaynak workspace/ içindedir. Core, kayıt, WinForms canlı/demo arayüzü, canlı bağlantı yönetimi ve testler bu proje için yazıldı. Referans uygulamanın arayüzü, DPS, Alerts veya boss mantığı alınmadı.

Referans: AION 2 Alerts 0.5.6, GPL-3.0 (`../reference/AION2Alerts-source/`). Referansın capture tabanı cyberbadger6969/aion2-dps-meter, commit 414de5c1d5f3d45ce35701bd25229c2dba736bb6 (GPL-3.0), https://github.com/cyberbadger6969/aion2-dps-meter . PetTracker.cs ve PacketPipeline.cs davranış incelemesi için okundu; dosyalar projeye alınmadı.

Uyarlanan kaynaklar: `Capture/TcpReassembler.cs` → `PetTracker.Capture/TcpReassembler.cs`; `Protocol/FrameDecoder.cs` ve `Protocol/Wire.cs` → aynı adlarla yeni namespace. Değişiklikler: namespace/atıf, taşan varint reddi, veri kaybından sonra heartbeat bulmadan tahmini frame çözümlememe. `GameConnections.cs` Windows TCP tablo sorgusu `Capture/GameProcessLocator.cs` temel alınarak port 13328/süreç sahipliği ile daraltıldı, tablo büyüme hataları raporlanıyor. `Protocol/PetProtocol.cs`, referans `PetPackets.cs` pet okuyucularının ve `PacketParser.cs` kendi karakter kimliği okuma alt kümesinin uyarlamasıdır; bilinmeyen sayısal ID desteği ve ayrıntılı doğrulama sebebi eklenmiştir. Referans gibi, eksiksiz doğrulanan pet listelerinden sonraki ek alanlar soul hesabına katılmaz. 13 Ekim 2026 kullanıcı collection mesajında 1536 bayt ek alan gözlendi; bu alanın içeriği pet ilerlemesi olarak yorumlanmaz. Yeniden birleştirme ve frame kodu GPL-3.0 kapsamındadır; proje bütün olarak GPL-3.0 dağıtılır. Referans kaynakları hiç değiştirilmedi.

Kopyalanan dosyalar: data/pet-catalog.json, data/PET-SOURCES.md ve ilgili lisans metinleri. Katalog 207 benzersiz ID içerir (7 Special); bu kayıtlar silinmedi ve oyunun 200 kayıt listesi olarak sunulmaz. Catalog, ID → ad etiketi sağlar. Referans PetTracker.cs'deki tam collection sonrası eksik level/soul kaydı için sıfır başlangıcı kuralı, yalnızca canlı ve kesintisiz tam snapshot sonrası gerçek soul kazanan bilinen Pet ID için uygulanır; tüm katalog ID'leri topluca sıfır progress ile doldurulmaz. Genel snapshot, bilinmeyen ID, eski kayıt ve veri kaybı bu kuralı açmaz.

Pet katalog kaynağı Questlog, xCROv/aion2-pet-tracker GPL-3.0 ağacı 93da8f0a4d672210eb455c20bbea7dcc9191cd5b. Konum yedek verileri MartinLVila/aion2-pet-overlay MIT ağacı 211ab2aa7216813dd913f4d8521d860e9ccb3e0e (MetaBot). Tam kaynak notları data/PET-SOURCES.md içindedir. 207 adet 64px PNG portre reference/data/pets/ içinden değiştirilmeden workspace/data/pets/ içine alındı. Bunlar Questlog oyun verisinin xCROv GPL-3.0 ağacındaki portreleridir; görseller NCSOFT ve ilgili hak sahiplerine aittir. Kaynak/işleme ayrıntıları data/PET-SOURCES.md ve ilgili licenses/ metinlerinde korunur.

Protokol kaynakları: elkaida/AION2-pet-tracker MIT ağacı 7a8afb636c0516f07cc45918687142d77c94bd5a ve xCROv GPL-3.0. Lisansları licenses/ içindedir. Referanstaki opcode yorumlarının güncel canlı sürümde kararlı olduğu iddia edilmez; testler sentetik ve çevrimdışıdır.

Oyun adları/verileri NCSOFT ve ilgili hak sahiplerine aittir. Proje bağımsızdır; resmi onay veya ilişki iddiası yoktur.

.NET ve System.* bağımlılıkları: Microsoft ve katkıcılar, MIT (`licenses/dotnet-MIT.txt`). Test araçları: xUnit ve Visual Studio runner (Apache-2.0), Microsoft.NET.Test.Sdk (MIT), coverlet.collector (MIT); NuGet test bağımlılıklarıdır.

Canlı çalışma zamanı NuGet bağımlılıkları: SharpPcap 6.3.1 (MIT, Tamir Gal/Chris Morgan ve katkıcılar), https://github.com/chmorgan/sharppcap/tree/bfedf297e7410ffcf44e05d14f7fbef304f20895 ; onun transitif PacketDotNet 1.4.8 (MPL-2.0), https://github.com/dotpcap/packetnet/tree/690707ce56d6e9c266daf6236c4f76ac5035334c ; K4os.Compression.LZ4 1.3.8 (MIT, Milosz Krajewski), https://github.com/MiloszKrajewski/K4os.Compression.LZ4 . Kütüphaneler değiştirilmeden ayrı DLL olarak bağlanır. Lisans metinleri licenses/ içindedir ve uygulama çıktısına kopyalanır. PacketDotNet'in MPL kapsamındaki karşılık gelen kaynak kodu yukarıdaki kaynak bağlantısından alınabilir.

Npcap sistem sürücüsü bu projede paketlenmez veya kurulmaz; kullanıcı ayrı kurar. Npcap lisansı ve kılavuzu: https://npcap.com/ ve https://npcap.com/guide/npcap-users-guide.html .

Overlay 0.3.0: koyu/altın WinForms arayüzü bu proje için yeniden yazıldı. PetTargetProtocol.cs, referans PacketParser.cs içindeki NPC spawn ve cast/hit kaynak-hedef okuma alt kümesinden uyarlanmıştır (GPL-3.0); hasar okuyucusu, DPS, Alerts veya boss kodu eklenmedi. Kendi saldırısı yalnızca kart görünürlüğünü değiştirir; soul sayaçları ayrı kalır. Son kullanıcı isteği, ilk aşamadaki yalnızca soul/pin görünürlüğünü bu şekilde genişletti.

Server-name lookup: data/servers.json copied unchanged from AION 2 Alerts reference data/servers.json; reference data/NOTICE.md attributes taengu/A2Tools-DPS-Meter src/data commit e32dc5e (2026-10-05), GPL-3.0, https://github.com/taengu/A2Tools-DPS-Meter . Game names and underlying data copyright NCSOFT. No DPS code was copied. AppBrand emblem, crystal cursor, startup effects and synthesized MaxChime are original code for this project.

Velopack 1.2.161 (MIT, Caelan Sayler / Velopack Ltd.), GitHub https://github.com/velopack/velopack commit 92d6a1c91716729d449034df5c50307dcce39493. SDK is used unchanged for GitHub release discovery, verified package downloads and restart/update. License: licenses/Velopack-MIT.txt. Publishing workflow uses matching vpk packaging tool. Updater traffic is HTTPS to GitHub; game capture remains passive and unchanged.

Interactive map assets and coordinates: xCROv/aion2-pet-tracker (GPL-3.0), commit 93da8f0a4d672210eb455c20bbea7dcc9191cd5b, https://github.com/xCROv/aion2-pet-tracker/tree/93da8f0a4d672210eb455c20bbea7dcc9191cd5b/app/src/renderer/public/maps . The sector bounds and normalized Y-axis projection follow the source `app/src/renderer/src/lib/spawns.ts`. Its GPL text is in licenses/xCROv-pet-tracker-GPL-3.0.txt. PNG assets are pixel-preserving format conversions of the original game map WebPs, which remain in the source tree. Map UI and camera are independently implemented in C#/WinForms; no Vue, PeakPathing frontend, DPS or Alerts UI code was incorporated. The underlying game images and data remain the property of NCSOFT and respective owners. Full provenance is in data/PET-SOURCES.md.

## Bundled UI fonts

Instrument Sans Regular/Bold by The Instrument Sans Project Authors, from https://github.com/Instrument/instrument-sans (revision in src/PetTracker.App/Assets/Fonts/InstrumentSans-source.txt). SIL Open Font License 1.1: licenses/InstrumentSans-OFL.txt.

Bebas Neue Regular V2.000 by Dharma Type, supplied by the user. SIL Open Font License 1.1: licenses/BebasNeue-OFL.txt. Fonts remain under their original licenses and are registered privately for this process only. Coolvetica, Akira Expanded Demo and Brunson are not bundled.
