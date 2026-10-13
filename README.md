# Aion 2 Pet Tracker — 0.7.0 / GitHub updates

Windows x64 pet soul tracker, GPL-3.0. Public update feed: https://github.com/keytftw/aion2pettracker

Açılış sırası: Checking Updates → varsa indirme (% ilerleme) ve otomatik yeniden açılma → 3.5sn açılış → 10 dil seçimi → koşul onayı → güncel collection için karakter seçiminden yeniden giriş. Güncelleme yoksa normal akış devam eder. İnternet/GitHub/paket hatası, "güncelleme yok" olarak sunulmaz; Tekrar Dene veya Mevcut Sürümle Devam seçilir. Oyun okumaları koşul onayından önce başlamaz.

Otomatik güncelleme yalnızca Releases'taki yeni Velopack portable ZIP / Setup dağıtımıyla çalışır. Önceki sıradan ZIP ve kökteki tek EXE'de Update.exe/sq.version yoktur; kullanıcı bir kez yeni pakete geçmelidir. GitHub güncellemeleri token istemez, yalnızca stable sürümleri izler. SDK paket bütünlüğü kontrolü yapar; indirilen EXE doğrudan çalıştırılmaz, Velopack güncelleme süreci kullanılır. .NET runtime dahildir. Npcap ayrı kullanıcı kurulumudur.

## Yeni release yayınlama

Bu depodaki Actions → Build and publish updater release → Run workflow.
Workflow Windows'ta build ve offline testleri yapar, self-contained publish üretir, vpk 1.2.161 ile portable ZIP/Setup ve update feed oluşturup GitHub Releases'a yayınlar. Kaynak ZIP de eklenir. Normal kod push'u yayınlamayı tetiklemez. Yeni sürüm için önce src/PetTracker.App/PetTracker.App.csproj içindeki Version değerini artırın. Aynı sürümü tekrar yayınlamayın.

Yerelde installer/packaged binary çalıştırılmadı. İlk GitHub release henüz oluşturulmadıysa otomatik güncelleme canlı değildir; release paketleme ve farklı bilgisayarda ardışık iki sürümle update/restart testi gereklidir. GitHub erişimi kesilirse mevcut sürüm kullanılabilir. Kullanıcı kayıtları %LOCALAPPDATA%/Aion2PetTracker altında korunur; güncelleme sonrası yine yeni collection gerekir. Kod imzası ve geniş dağıtım için yayıncı kimliği ayrıca değerlendirilmeli.

## Geliştirme

.NET 10 SDK, Windows x64:

```powershell
dotnet build PetTracker.slnx -c Release
dotnet test PetTracker.slnx -c Release --no-build
dotnet publish src/PetTracker.App/PetTracker.App.csproj -p:PublishProfile=Portable
```

PublishProfile yalnızca build çıktısı üretir; updater-enabled dağıtım CI'da vpk paketlemesiyle oluşturulur. Demo ve bakım seçenekleri GitHub/capture çalıştırmaz. Kaynak, portre ve sunucu verisi atıfları THIRD_PARTY_NOTICES.md, data/PET-SOURCES.md ve licenses/ içindedir. Gelişmiş kayıt/protokol ve kullanım ayrıntıları aşağıdadır.

## Veri ve mantık

- `AuthoritativeCollectionSnapshot`: son doğrulanmış tam collection; sonraki gain'ler bu nesneyi değiştirmez.
- `LastKnownTotals`: snapshot ve güvenilir gain'lerden oluşan son bilinen toplamlar.
- `SessionActivity` / `SoulEvent`: yeni oturum gain'leri ve aktif petler. Uygulama tekrar açıldığında oturum listesi temiz başlar.
- `UIPreferences`: pin/kaldır ID'leri karakter bazında saklanır; snapshot bunları silmez.
- `DataQuality`: snapshot bekleniyor / kesin / belirsiz. Yeniden açılan kayıt yeni snapshot olmadan kesin sayılmaz.

Lv0 toplam 0–4; Lv1 5–29; Lv2 30–104; Lv3 105+ MAX. Aşama hedefleri 5/25/75. MAX'tan sonra soul olayı gözlenirse doğrulama istenir. Shard mesajı sayılmaz; Saldırı yalnızca kart görünürlüğünü etkiler; kill sayacı yoktur.

Olaylar, karakter/sunucu + akış ID + monoton artan frame ID taşır. Aynı frame tekrar uygulanmaz; aynı Pet ID/miktar taşıyan iki farklı frame ikisi de sayılır. Eski frame loglanır. Sıralı akış beklenir; frame numaralarının boşluğu tek başına soul kaybı kanıtı değildir (başka mesajlar olabilir). Gerçek kayıp ayrı Gap sinyaliyle bildirilir. Canlı mod frame kimliğini TCP yeniden birleştirmesinden sonra üretir.

Genel/önbellekten gelen veya eksik snapshot'ta bulunmayan ID'lerin sıfır olduğu varsayılmaz. Global'in doğrulanmış tam collection'ı seyrektir: seviye/soul listelerinde olmayan petin başlangıcı sıfırdır (referans PetTracker.cs aynı kuralı kullanır). Bu kural yalnızca aynı kesintisiz akışta gerçek soul kazanan, katalogda bilinen Pet ID için uygulanır. 207 katalog kaydı topluca sıfır sayaçlarla doldurulmaz. Bilinmeyen ID, paket kaybı veya reconnect sonrasında sıfır başlangıcı üretilmez. `AuthoritativeCollectionSnapshot.OmittedEntriesAreZero` özelliği yalnızca bu doğrulanmış tam protokol snapshot'ı için açılır; eski kayıtlar/generic snapshot'lar varsayılan olarak kapalıdır.

Katalog 207 benzersiz kayıt/7 Special içerir; oyunun 200 kayıt listesine doğrulanmış eşleme yoktur. Tüm katalog adları seçim için tutulur, yeni soul alan, kendi saldırısıyla etkinleşen veya pinlenen ID'ler tabloda görünür.

Pinlenen normal katalog peti tam, seyrek collection'ın iki listesinde de yoksa Lv0 0/5 (kalan 5) gösterilir. Bu salt okunur snapshot yorumudur: Pin sayaçlara veya oturum soul olaylarına yazmaz. Veri kaybında/yeniden açılışta aynı değer yalnızca “Son bilinen” olur. Henüz oyun snapshot'ı veya soul olayıyla gözlenmemiş 7 Special kayıt ve katalog dışı ID'ler otomatik sıfır kabul edilmez; bilinmiyor kalır.

Demo kayıt: `%LOCALAPPDATA%\Aion2PetTracker\Demo\state.json`, yedek `state.json.bak`; anlaşılır log `tracker.log`. Her işlem ve kapanışta atomik geçici dosya + replace ile kayıt yapılır; canlı olaylar 250 ms aralıklarla toplu işlenip kaydedilir. Bozuk kayıt sessizce sıfırlanmaz/üzerine yazılmaz; uygulama açıklayıcı hata verir. Dosyayı yedekleyip yeniden adlandırarak temiz demo başlatabilirsiniz. Demo ve canlı profiller ayrı klasörde tutulur.

## Dosyalar

`src/PetTracker.Core`: bağımsız mantık ve JSON kayıt. `src/PetTracker.Capture`: pasif canlı bağlantı ve pet protokolü. `src/PetTracker.App`: Türkçe canlı/demo WinForms. `tests/PetTracker.Tests`: otomatik çevrimdışı testler. `data`: pet ad kataloğu ve kaynak notları. `LICENSE`, `THIRD_PARTY_NOTICES.md`, `licenses/`: GPL-3.0 ve kaynak atıfları.

Referans kaynak değiştirilmedi. Canlı aşama kullanıcı tarafından onaylandı. Yalnızca gereken TCP/frame/wire parçaları ve pet/kimlik okuma alt kümesi lisanslarıyla birlikte uyarlanmıştır; DPS/Alerts/boss mantığı eklenmedi. Ayrıntılar THIRD_PARTY_NOTICES.md içinde.

## Doğrulama ve sınırlar

`dotnet build` ve `dotnet test` deterministik, sentetik akışlarla doğrulanır. Testler TCP tekrar/sırasızlık/kayıp, bölünmüş ve LZ4 frame'leri, bozuk collection, shardların yok sayılması ve saldırıların soul saymaması, kimlik öncesi tamponlama, karakter/sunucu izolasyonu, pin/kaldır ve JSON kayıt davranışlarını kapsar. UI açılışı ayrıca `PetTracker.App.exe --smoke-check` ile canlı ve demo formu oluşturularak kontrol edilir; bu kontrol capture başlatmaz ve test dosyalarını exe yanındaki `smoke-check` klasörüne yazar.

Gerçek Global oyun trafiğiyle doğrulama henüz yapılmadı. Bu nedenle doğru çalıştığı oyun içinde bir tam collection ve gerçek soul kazanımı gözlenerek kontrol edilmelidir. “Kimlik/collection bekleniyor” devam ederse logdaki mesajı paylaşın; kesin görünmesi için eski veya demo sayaçları canlıya taşımayın.

## Collection tanısı

`Collection-Tani.cmd` yalnızca collection biçim hatasını incelemek için hazırlanmış ayrı, elle başlatılan tanı aracıdır. 120 saniye pasif okur; karaktere bu sırada tekrar giriş yapın. En fazla 8 adet yalnızca `0x0090` mesaj gövdesi ve hata sebebi `workspace/diagnostics/collection-local/` altına kaydedilir. Tam ağ trafiği, kimlik bilgileri veya oyun girdisi kaydedilmez; sayaç/profil dosyaları değişmez. Ana uygulamada bu kayıt kapalıdır. Collection verisi pet ilerlemenizi içerir ve yerelde tutulur; paylaşmanız gerekmez. Tanı çıktıları kişisel olduğundan sürüm kontrolüne eklenmemelidir.

Tanı aracını yeniden derleme:

```powershell
dotnet build tools\PetTracker.Diagnostics\PetTracker.Diagnostics.csproj -c Release -p:BaseOutputPath=bin/diagnostics/
```
