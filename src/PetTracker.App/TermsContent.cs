namespace PetTracker.App;

// Independently drafted informational terms. No GPL rights are surrendered by acceptance.
// Legal enforceability depends on the user's jurisdiction. Review before public distribution.
public static class TermsContent
{
    public const string Version="2026-10-13 / 1.0";
    public const string PublisherPolicy="https://www.plaync.com/policy/operation/aion2global/en";
    private static readonly Dictionary<string,string> Bodies=new()
    {
        ["en"]="""
1. Independent application
Aion 2 Pet Tracker is an unofficial community utility. Keyt and the contributors are not affiliated with, endorsed by or acting for NC or the AION 2 publisher. Names, portraits and trademarks belong to their respective owners. This is not an official permission to use third-party software.

2. Account sanctions — explicit risk acceptance
Third-party tools can expose your account to investigation, temporary restriction, permanent ban, loss of items or purchases and loss of access. Passive reading does not guarantee approval, safety or invisibility to anti-cheat. By proceeding you knowingly accept these risks. Keyt and contributors do not control publisher decisions and, to the fullest extent permitted by applicable law, are not responsible for bans, account restrictions or resulting losses. No developer can promise a ban-free application.

3. Publisher rules and lawful use
Review the current game terms and operation policy before each use. Stop using the utility if it is prohibited or you are unsure. You must have authority to use the device and read the selected connection. Do not use this software to evade protection, cheat, exploit, automate gameplay or access someone else's account. We do not grant any right to violate laws or game rules.

4. Technical operation and prerequisites
Live mode passively reads supported server-to-client traffic using separately installed Npcap. It does not send game packets, inject into the game or generate inputs. Administrator permissions depend on your Npcap installation. Windows, network topology and game updates can prevent operation. You install and maintain prerequisites yourself; never disable security protections to use this utility.

5. Accuracy and fresh synchronization
Counts reflect observed soul gains, not kills. Packet loss, protocol changes, disconnections and unsupported IDs can cause missing or uncertain data. Every launch requires new character identification and a validated collection. Cached values are not treated as current truth. Verify important numbers in-game; do not rely on this tool for financial or irreversible decisions.

6. Local data and privacy
The application stores character/server identifiers, pet counters, preferences and diagnostic logs locally under your Windows user profile. It does not ask for passwords or send these records to a developer service. The current application does not write raw packet captures. Log files can still contain identifying information: inspect and redact them before sharing. Other software and Npcap have their own policies. You may remove the local application-data folder after closing the utility; this deletes saved preferences and records.

7. No warranties
The software is supplied as-is and as-available, without a promise of accuracy, uninterrupted operation, compatibility, merchantability, fitness for a particular purpose or continued support, to the extent permitted by law. Updates may change or discontinue features. Keep independent backups of files that matter.

8. Limits of liability
To the extent lawful, Keyt and contributors exclude liability for indirect, incidental, consequential losses, lost time, lost data, lost items, lost revenue and sanctions related to use or inability to use this application. These terms do not exclude liability that cannot legally be excluded, including mandatory consumer rights, fraud or other non-waivable obligations. Acceptance does not make every disclaimer enforceable.

9. License and distribution
The application is distributed under GPL-3.0; LICENSE and third-party notices govern the respective components. These terms do not restrict rights granted by that license. Preserve required notices and provide corresponding source when distributing as required by GPL. Pet assets have separate attribution and rights; do not represent them as your own or as publisher endorsement.

10. Changes, refusal and ending use
You must review and accept the displayed terms on every launch; consent is not reused from a previous session. If you disagree, choose Close. Close also stops capture. Reopening requires fresh consent and synchronization. Continued availability, game compatibility and account outcomes are not guaranteed.

11. Interpretation and legal protections
Translations support understanding and do not waive mandatory protections in your jurisdiction. If a clause is invalid, the lawful remaining provisions continue where permitted. No forum, governing law or operator business address is invented here. This text is a draft requiring jurisdiction-specific legal review before public release; consult a qualified professional if needed.

12. Your acknowledgement
By checking the box and choosing ACCEPT, you confirm that you have read these terms, understand that account sanctions including permanent bans are possible, choose to proceed voluntarily and have authority to accept. If you cannot understand or accept the risks, do not continue. Publisher rules remain authoritative and may change.
""",
        ["tr"]="""
1. Bağımsız uygulama
Aion 2 Pet Tracker resmi olmayan bir topluluk aracıdır. Keyt ve katkıda bulunanlar NC veya AION 2 yayıncısına bağlı değildir, onlar adına hareket etmez ve resmi onay iddia etmez. İsim, portre ve markalar sahiplerine aittir. Bu metin üçüncü taraf yazılım kullanma izni değildir.

2. Hesap yaptırımları ve açık risk kabulü
Üçüncü taraf araç kullanımı inceleme, geçici kısıtlama, kalıcı ban, eşya veya satın alım kaybı ve hesap erişiminin kaybına yol açabilir. Pasif okuma resmi onay, güvenlik veya anti-cheat tarafından görünmezlik garantisi vermez. Devam ederek bu riskleri bilerek kabul edersiniz. Keyt ve katkıda bulunanlar yayıncının kararlarını kontrol etmez; yürürlükteki hukukun izin verdiği en geniş ölçüde ban, hesap kısıtlaması ve bunlardan doğan kayıplardan sorumlu değildir. Hiçbir geliştirici bansız kullanım sözü veremez.

3. Oyun kuralları ve hukuka uygun kullanım
Her kullanım öncesinde güncel oyun sözleşmesini ve işletim politikasını inceleyin. Yasaksa veya emin değilseniz kullanmayın. Cihazı ve seçilen bağlantıyı okumaya yetkiniz olmalıdır. Koruma aşma, hile, exploit, oyun otomasyonu veya başkasının hesabına erişim için kullanmayın. Bu koşullar kanunu ya da oyun kurallarını ihlal etme hakkı vermez.

4. Teknik çalışma ve gereksinimler
Canlı mod, ayrı kurulan Npcap ile desteklenen sunucu→istemci trafiğini pasif okur. Oyun paketi göndermez, oyuna injection yapmaz veya girdi üretmez. Yönetici ihtiyacı Npcap kurulumuna bağlıdır. Windows, ağ yapısı veya oyun güncellemeleri çalışmayı engelleyebilir. Gereksinimleri kendiniz kurup yönetirsiniz; aracı kullanmak için güvenlik korumalarını kapatmayın.

5. Doğruluk ve yeni senkronizasyon
Sayaçlar kill değil gözlenen soul kazanımını temsil eder. Paket kaybı, protokol değişikliği, kopma veya desteklenmeyen ID eksik/belirsiz veri yaratabilir. Her açılış yeni karakter kimliği ve doğrulanmış collection ister. Eski kayıt güncel gerçek kabul edilmez. Önemli sayıları oyun içinde kontrol edin; mali veya geri alınamaz kararları bu araca dayandırmayın.

6. Yerel veri ve gizlilik
Karakter/sunucu kimliği, pet sayaçları, tercihler ve tanı logları Windows kullanıcı profilinizde yerel saklanır. Uygulama şifre istemez ve bu kayıtları geliştirici servisine göndermez. Mevcut sürüm ham paket kaydı yazmaz. Loglar yine de tanımlayıcı bilgi içerebilir; paylaşmadan önce inceleyip maskeleyin. Npcap ve diğer yazılımların kendi koşulları vardır. Kapattıktan sonra uygulama veri klasörünü silmek kayıt ve tercihleri kaldırır.

7. Garanti verilmemesi
Yazılım, hukukun izin verdiği ölçüde olduğu gibi ve mevcut haliyle sunulur. Kesin doğruluk, kesintisizlik, uyumluluk, ticarete elverişlilik, belirli amaca uygunluk veya sürekli destek sözü yoktur. Güncellemeler özellikleri değiştirebilir/kaldırabilir. Önemli dosyaları bağımsız yedekleyin.

8. Sorumluluğun sınırı
Hukuken mümkün ölçüde Keyt ve katkıda bulunanlar kullanım veya kullanamama nedeniyle dolaylı, arızi, sonuçsal zararlar, zaman/veri/eşya/gelir kaybı ve yaptırımlardan sorumlu tutulmaz. Kanunen dışlanamayan sorumluluklar, zorunlu tüketici hakları, hile ve vazgeçilemeyen yükümlülükler saklıdır. Kabul etmek her sorumsuzluk maddesini otomatik geçerli kılmaz.

9. Lisans ve dağıtım
Uygulama GPL-3.0 kapsamında dağıtılır; LICENSE ve üçüncü taraf bildirimleri ilgili bileşenleri yönetir. Bu koşullar lisans haklarını kısıtlamaz. Dağıtımda gerekli bildirimleri koruyun ve GPL'nin istediği karşılık gelen kaynak kodunu sağlayın. Pet görsellerinin ayrı atıf ve hakları vardır; size ait veya yayıncı onaylı gibi sunmayın.

10. Değişiklik, ret ve kullanımın bitişi
Her açılışta gösterilen koşulları yeniden okuyup kabul etmelisiniz; eski onay kullanılmaz. Kabul etmiyorsanız Kapat seçin. Kapatmak okumayı da durdurur. Yeniden açılış yeni onay ve senkronizasyon gerektirir. Sürekli erişim, oyun uyumluluğu ve hesap sonuçları garanti edilmez.

11. Yorum ve hukuki güvenceler
Çeviriler anlamayı destekler; ülkenizdeki zorunlu hakları kaldırmaz. Geçersiz madde varsa hukuken mümkün kalan maddeler uygulanır. Burada yetkili mahkeme, uygulanacak hukuk veya işletmeci adresi uydurulmamıştır. Bu taslak halka dağıtımdan önce ülkeye özel hukuk incelemesi gerektirir; gerektiğinde uzman desteği alın.

12. Beyanınız
Kutuyu işaretleyip KABUL ET seçerek metni okuduğunuzu, kalıcı ban dahil hesap yaptırımı riskini anladığınızı, gönüllü devam ettiğinizi ve kabul yetkiniz bulunduğunu beyan edersiniz. Riskleri anlamıyor veya kabul etmiyorsanız devam etmeyin. Yayıncının kuralları belirleyicidir ve değişebilir.
""",
        ["de"]="""
1. Unabhängige Software
Aion 2 Pet Tracker ist ein inoffizielles Community-Werkzeug. Keyt und Mitwirkende sind weder mit NC verbunden noch vom Herausgeber bestätigt. Namen, Bilder und Marken gehören ihren Eigentümern. Diese Bedingungen sind keine offizielle Nutzungserlaubnis.

2. Kontosperren und bewusste Risikoübernahme
Drittsoftware kann Ermittlungen, zeitweise Einschränkungen, dauerhafte Sperren sowie Verlust von Gegenständen, Käufen und Kontozugang verursachen. Passives Lesen garantiert weder Erlaubnis noch Sicherheit oder Unsichtbarkeit für Anti-Cheat. Sie übernehmen diese Risiken bewusst. Keyt und Mitwirkende kontrollieren keine Herausgeberentscheidungen und haften, soweit gesetzlich zulässig, nicht für Sperren, Einschränkungen oder Folgeschäden. Niemand verspricht eine sperrfreie Nutzung.

3. Rechtmäßige Nutzung
Prüfen Sie aktuelle Spielbedingungen und Betriebsrichtlinien vor jeder Nutzung. Bei Verbot oder Unsicherheit nicht fortfahren. Sie benötigen die Berechtigung für Gerät und Verbindung. Keine Umgehung von Schutzmaßnahmen, Cheats, Exploits, Automatisierung oder fremden Konten. Es wird kein Recht zum Regel- oder Gesetzesverstoß eingeräumt.

4. Funktionsweise
Live-Modus liest unterstützten Server-zu-Client-Verkehr passiv mit separat installiertem Npcap. Keine Spielpakete werden gesendet, keine Injection oder Spieleingaben erzeugt. Administratorrechte hängen von Npcap ab. System-, Netzwerk- oder Spieländerungen können die Funktion verhindern. Sicherheitsmaßnahmen nicht abschalten.

5. Genauigkeit
Zähler zählen beobachtete Seelen, keine Tötungen. Paketverlust, Verbindungsabbruch, Protokolländerungen oder unbekannte IDs können Daten unsicher machen. Jeder Start erfordert neue Charakteridentifikation und eine validierte Sammlung; gespeicherte Zahlen gelten nicht als aktuell. Wichtige Werte im Spiel überprüfen, keine finanziellen oder unumkehrbaren Entscheidungen darauf stützen.

6. Lokale Daten
Charakter-/Serverkennungen, Zähler, Einstellungen und Diagnoseprotokolle werden lokal im Windows-Profil gespeichert. Keine Passwortabfrage oder Übertragung an einen Entwicklerdienst; diese Version schreibt keine Rohpaketaufzeichnungen. Protokolle vor Weitergabe prüfen und anonymisieren. Npcap und andere Software haben eigene Richtlinien. Nach Beenden können Sie den Datenordner löschen; gespeicherte Daten und Einstellungen gehen verloren.

7. Keine Garantie
Die Software wird wie vorhanden und verfügbar angeboten, soweit gesetzlich zulässig ohne Zusicherung der Genauigkeit, Verfügbarkeit, Kompatibilität, Marktgängigkeit, Eignung oder fortlaufenden Unterstützung. Funktionen können geändert oder entfernt werden. Wichtige Dateien unabhängig sichern.

8. Haftungsgrenzen
Soweit rechtlich zulässig, keine Haftung für mittelbare, beiläufige oder Folgeschäden, Zeit-, Daten-, Gegenstands- oder Einnahmeverlust sowie Sanktionen durch Nutzung oder Nichtnutzbarkeit. Zwingende Verbraucherrechte, Betrug und nicht ausschließbare Haftung bleiben unberührt. Zustimmung macht unzulässige Klauseln nicht wirksam.

9. Lizenz
GPL-3.0, LICENSE und Drittanbieterhinweise gelten für die jeweiligen Komponenten. Diese Bedingungen beschneiden keine GPL-Rechte. Bei Weitergabe erforderliche Hinweise und entsprechenden Quellcode bereitstellen. Bildrechte und Attribution beachten; keine Eigentums- oder offizielle Billigungsbehauptung.

10. Ablehnung und Ende
Bei jedem Start erneut lesen und zustimmen; frühere Zustimmung wird nicht übernommen. Andernfalls Schließen wählen. Schließen beendet die Erfassung. Neustart erfordert Zustimmung und Synchronisierung. Verfügbarkeit und Kontoergebnisse sind nicht garantiert.

11. Rechtliche Auslegung
Übersetzungen helfen beim Verständnis; zwingendes örtliches Recht bleibt bestehen. Unwirksame Teile beeinträchtigen übrige zulässige Teile nicht, soweit erlaubt. Kein Gerichtsstand, anwendbares Recht oder Betreiberadresse wird erfunden. Vor öffentlicher Veröffentlichung ist dieser Entwurf juristisch und sprachlich zu prüfen.

12. Bestätigung
Mit Kontrollkästchen und AKZEPTIEREN bestätigen Sie Lesen, Verständnis des dauerhaften Sperrrisikos, freiwilliges Fortfahren und Annahmebefugnis. Bei Unverständnis oder Ablehnung nicht fortfahren. Herausgeberregeln bleiben maßgeblich und können sich ändern.
""",
        ["ru"]="""
1. Независимое приложение
Aion 2 Pet Tracker — неофициальный инструмент сообщества. Keyt и участники не связаны с NC, не представляют издателя и не заявляют о его одобрении. Названия, изображения и товарные знаки принадлежат владельцам. Эти условия не являются разрешением издателя.

2. Санкции и принятие риска
Сторонние программы могут привести к проверке аккаунта, временным ограничениям, постоянному бану, потере предметов, покупок и доступа. Пассивное чтение не гарантирует разрешения, безопасности или незаметности для античита. Продолжая, вы осознанно принимаете риски. Keyt и участники не контролируют решения издателя и, в максимально допустимых законом пределах, не отвечают за блокировки, ограничения и связанные потери. Гарантии отсутствия бана нет.

3. Законное использование
Перед каждым запуском проверяйте актуальные правила игры. При запрете или сомнениях прекратите использование. Требуется право пользоваться устройством и читать выбранное соединение. Запрещено применять инструмент для обхода защиты, читов, эксплуатации ошибок, автоматизации или доступа к чужим аккаунтам. Права нарушать закон или правила не предоставляются.

4. Техническая работа
Live-режим пассивно читает поддерживаемый трафик сервер→клиент через отдельно установленный Npcap. Игровые пакеты не отправляются, внедрения и игровых вводов нет. Права администратора зависят от установки Npcap. Изменения Windows, сети или игры могут нарушить работу. Не отключайте защиту системы ради приложения.

5. Точность
Считаются наблюдаемые души, не убийства. Потери пакетов, отключения, смена протокола и неизвестные ID могут вызвать пропуски и неопределённость. Каждый запуск требует новой идентификации персонажа и проверенной коллекции. Старые числа не считаются актуальными. Проверяйте важные значения в игре; не основывайте финансовые или необратимые решения на трекере.

6. Локальные данные
Идентификаторы персонажа/сервера, счётчики, настройки и журналы хранятся локально в профиле Windows. Пароли не запрашиваются, данные не отправляются сервису разработчика; текущая версия не пишет сырые сетевые пакеты. Перед передачей журналов проверьте и удалите личные сведения. Npcap и другие программы имеют собственные политики. Удаление папки данных после закрытия удаляет записи и настройки.

7. Отсутствие гарантий
ПО предоставляется как есть и по доступности без гарантии точности, непрерывности, совместимости, пригодности для продажи или конкретной цели, постоянной поддержки — насколько допускает закон. Функции могут изменяться или исчезнуть. Делайте независимые резервные копии важных файлов.

8. Ограничение ответственности
В допустимых законом пределах исключается ответственность Keyt и участников за косвенные, случайные и последующие убытки, потерю времени, данных, предметов, дохода и санкции из-за использования или невозможности использования. Неотменяемые права потребителя, мошенничество и обязательная ответственность не исключаются. Согласие не делает незаконные оговорки действительными.

9. Лицензия
GPL-3.0, LICENSE и уведомления третьих лиц регулируют соответствующие компоненты. Эти условия не ограничивают права GPL. При распространении сохраняйте уведомления и предоставляйте соответствующий исходный код согласно лицензии. Соблюдайте права на изображения; не выдавайте их за свои или официальное одобрение.

10. Отказ и завершение
Условия принимаются заново при каждом запуске, прежнее согласие не используется. При несогласии нажмите Закрыть; захват тоже прекращается. Повторное открытие требует согласия и синхронизации. Постоянная доступность и результаты для аккаунта не гарантированы.

11. Правовое толкование
Переводы помогают пониманию, обязательные местные права сохраняются. Недействительная часть не отменяет остальные допустимые части, если закон позволяет. Суд, применимое право и адрес оператора здесь не выдумываются. Перед публичным выпуском проект требует юридической и языковой проверки.

12. Подтверждение
Отмечая поле и выбирая ПРИНЯТЬ, вы подтверждаете чтение, понимание риска постоянного бана, добровольное продолжение и право согласиться. Если не понимаете или не принимаете риски, остановитесь. Правила издателя определяющие и могут изменяться.
""",
        ["es"]="""
1. Aplicación independiente
Aion 2 Pet Tracker es una herramienta comunitaria no oficial. Keyt y los colaboradores no están afiliados a NC ni respaldados por el editor. Nombres, imágenes y marcas pertenecen a sus titulares. Estas condiciones no autorizan oficialmente el uso de software externo.

2. Sanciones y aceptación del riesgo
Las herramientas externas pueden provocar investigación, restricciones temporales, bloqueo permanente, pérdida de objetos, compras y acceso. La lectura pasiva no garantiza permiso, seguridad ni invisibilidad ante el anticheat. Al continuar acepta conscientemente estos riesgos. Keyt y los colaboradores no controlan las decisiones del editor y, hasta el máximo permitido por la ley, no responden por bloqueos, restricciones ni pérdidas resultantes. No se promete ausencia de ban.

3. Uso legal
Revise las reglas actuales del juego antes de cada uso. No continúe si está prohibido o tiene dudas. Debe estar autorizado a usar el dispositivo y leer la conexión. No evada protecciones, haga trampas, explote errores, automatice el juego ni acceda a cuentas ajenas. No se concede permiso para infringir leyes o reglas.

4. Funcionamiento
El modo en vivo lee pasivamente tráfico compatible del servidor al cliente mediante Npcap instalado aparte. No envía paquetes de juego, inyecta código ni genera entradas. Los permisos administrativos dependen de Npcap. Cambios en Windows, red o juego pueden impedir el funcionamiento. No desactive medidas de seguridad.

5. Exactitud
Se cuentan almas observadas, no muertes. Pérdida de paquetes, desconexiones, cambios de protocolo e ID desconocidos pueden producir datos incompletos o inciertos. Cada inicio necesita identificación nueva y colección validada; los valores guardados no son verdad actual. Verifique cifras importantes en el juego y no base decisiones económicas o irreversibles en ellas.

6. Datos locales
Identificadores de personaje/servidor, contadores, preferencias y registros se guardan en su perfil de Windows. No se solicitan contraseñas ni se envían estos datos a un servicio del desarrollador. La versión actual no guarda paquetes sin procesar. Revise y anonimize registros antes de compartirlos. Npcap y otros programas tienen políticas propias. Puede borrar la carpeta de datos tras cerrar; perderá registros y preferencias.

7. Sin garantías
El software se ofrece tal cual y según disponibilidad, sin garantía de exactitud, continuidad, compatibilidad, comerciabilidad, idoneidad o soporte permanente, dentro de lo permitido por ley. Las actualizaciones pueden modificar o eliminar funciones. Mantenga copias independientes de archivos importantes.

8. Responsabilidad limitada
Hasta donde permite la ley, Keyt y colaboradores excluyen responsabilidad por daños indirectos, incidentales o consecuentes, pérdida de tiempo, datos, objetos, ingresos y sanciones derivados del uso o imposibilidad de uso. No se excluyen derechos obligatorios del consumidor, fraude ni obligaciones irrenunciables. La aceptación no valida cláusulas ilegales.

9. Licencia
GPL-3.0, LICENSE y avisos de terceros regulan sus componentes. Estas condiciones no limitan derechos GPL. Al distribuir conserve avisos y facilite código fuente correspondiente según la licencia. Respete atribuciones y derechos de imágenes; no afirme propiedad ni aprobación oficial.

10. Rechazo y finalización
Debe aceptar el texto mostrado en cada inicio; no se reutiliza consentimiento previo. Si discrepa elija Cerrar, que también detiene la captura. Reabrir requiere consentimiento y sincronización nuevos. No se garantiza disponibilidad ni resultado de la cuenta.

11. Interpretación jurídica
Las traducciones facilitan comprensión y no eliminan derechos locales obligatorios. Una cláusula inválida no afecta a las restantes válidas donde la ley lo permita. No se inventan jurisdicción, ley aplicable ni dirección del operador. Este borrador necesita revisión jurídica y lingüística antes de distribución pública.

12. Confirmación
Al marcar la casilla y ACEPTAR confirma lectura, comprensión del riesgo de bloqueo permanente, decisión voluntaria y facultad para aceptar. Si no comprende o acepta el riesgo, no continúe. Las reglas del editor prevalecen y pueden cambiar.
""",
        ["fr"]="""
1. Application indépendante
Aion 2 Pet Tracker est un outil communautaire non officiel. Keyt et les contributeurs ne sont ni affiliés à NC ni agréés par l’éditeur. Noms, portraits et marques appartiennent à leurs titulaires. Ce texte n’est pas une autorisation officielle.

2. Sanctions et acceptation du risque
Un logiciel tiers peut entraîner une enquête, des restrictions temporaires, un bannissement permanent, la perte d’objets, d’achats ou d’accès. La lecture passive ne garantit ni autorisation, ni sécurité, ni invisibilité à l’anti-triche. Vous acceptez consciemment ces risques. Keyt et les contributeurs ne contrôlent pas les décisions de l’éditeur et ne répondent pas, dans la mesure permise par la loi, des bannissements, restrictions et pertes associées. Aucune absence de ban n’est promise.

3. Usage licite
Vérifiez les règles actuelles du jeu avant chaque usage. En cas d’interdiction ou de doute, arrêtez. Vous devez être autorisé à utiliser l’appareil et lire la connexion. Aucun contournement, triche, exploitation de bugs, automatisation ou accès à un compte tiers. Aucun droit de violer les lois ou règles n’est accordé.

4. Fonctionnement
Le mode direct lit passivement le trafic serveur→client compatible via Npcap installé séparément. Il n’envoie pas de paquets de jeu, n’injecte pas de code et ne génère pas de commandes. Les privilèges dépendent de Npcap. Des changements système, réseau ou jeu peuvent empêcher le fonctionnement. Ne désactivez pas la sécurité.

5. Exactitude
Les compteurs représentent les âmes observées, pas les éliminations. Pertes de paquets, déconnexions, changements de protocole et ID inconnus peuvent rendre les données incomplètes. Chaque lancement exige une nouvelle identité et une collection validée ; les anciennes valeurs ne sont pas actuelles. Vérifiez dans le jeu et évitez toute décision financière ou irréversible fondée sur l’outil.

6. Données locales
Identifiants personnage/serveur, compteurs, préférences et journaux sont stockés dans votre profil Windows. Aucun mot de passe demandé ni transmission à un service du développeur. La version actuelle n’enregistre pas de paquets bruts. Vérifiez et anonymisez les journaux avant partage. Npcap et autres logiciels ont leurs propres politiques. Supprimer le dossier après fermeture efface données et préférences.

7. Absence de garantie
Logiciel fourni en l’état et selon disponibilité, sans garantie d’exactitude, continuité, compatibilité, qualité marchande, adaptation à un usage ou support permanent, dans les limites légales. Les fonctions peuvent changer ou disparaître. Sauvegardez séparément les fichiers importants.

8. Responsabilité
Dans les limites autorisées, Keyt et les contributeurs excluent les dommages indirects, accessoires ou consécutifs, pertes de temps, données, objets, revenus et sanctions liées à l’usage ou son impossibilité. Les droits impératifs du consommateur, la fraude et les responsabilités légalement non excluables demeurent. L’acceptation ne valide pas une clause illicite.

9. Licence
GPL-3.0, LICENSE et notices tierces régissent les composants concernés. Ces conditions ne limitent pas les droits GPL. En redistribution, conservez les notices et fournissez le code source correspondant requis. Respectez droits et attributions des images, sans prétendre en être propriétaire ou agréé.

10. Refus et fin
Lisez et acceptez à chaque ouverture ; le consentement antérieur n’est pas repris. Sinon, Fermer arrête aussi la capture. Un nouveau lancement exige accord et synchronisation. Disponibilité et résultats du compte non garantis.

11. Interprétation
Les traductions facilitent la compréhension ; les protections locales impératives restent applicables. Une clause invalide laisse les clauses licites restantes lorsque permis. Aucun tribunal, droit applicable ou adresse d’exploitant n’est inventé. Ce projet nécessite une vérification juridique et linguistique avant diffusion publique.

12. Reconnaissance
Cocher et ACCEPTER confirme lecture, compréhension du risque de bannissement permanent, choix volontaire et capacité à accepter. Sinon, ne continuez pas. Les règles de l’éditeur sont déterminantes et peuvent changer.
""",
        ["pt"]="""
1. Aplicativo independente
Aion 2 Pet Tracker é uma ferramenta comunitária não oficial. Keyt e colaboradores não são afiliados à NC nem aprovados pela editora. Nomes, retratos e marcas pertencem aos respectivos titulares. Este texto não é autorização oficial.

2. Sanções e aceitação do risco
Ferramentas externas podem causar investigação, restrição temporária, banimento permanente e perda de itens, compras e acesso. Leitura passiva não garante permissão, segurança ou invisibilidade ao anticheat. Ao continuar, você assume conscientemente estes riscos. Keyt e colaboradores não controlam a editora e, no máximo permitido por lei, não são responsáveis por banimentos, restrições e perdas decorrentes. Não há promessa de uso sem ban.

3. Uso legal
Leia as regras atuais do jogo a cada uso. Se proibido ou incerto, pare. Você precisa de autorização para o dispositivo e a conexão. Não contorne proteções, use cheats, explore falhas, automatize o jogo ou acesse contas alheias. Nenhum direito de violar leis ou regras é concedido.

4. Funcionamento
O modo ao vivo lê passivamente tráfego servidor→cliente compatível com Npcap instalado separadamente. Não envia pacotes de jogo, injeta código ou gera comandos. Permissões administrativas dependem do Npcap. Alterações no sistema, rede ou jogo podem impedir o funcionamento. Não desative proteções de segurança.

5. Precisão
Contadores mostram almas observadas, não abates. Perda de pacotes, desconexões, alterações de protocolo e IDs desconhecidos podem causar dados incompletos ou incertos. Todo início exige identidade nova e coleção validada; valores antigos não são atuais. Confira números no jogo e não baseie decisões financeiras ou irreversíveis na ferramenta.

6. Dados locais
Identificadores de personagem/servidor, contadores, preferências e logs ficam no perfil Windows. Não pedimos senhas nem enviamos esses registros a um serviço do desenvolvedor. A versão atual não grava pacotes brutos. Revise e anonimize logs antes de compartilhar. Npcap e outros programas têm políticas próprias. Apagar a pasta após fechar remove dados e preferências.

7. Sem garantias
Software fornecido como está e conforme disponível, sem garantias de precisão, continuidade, compatibilidade, comercialização, adequação ou suporte contínuo, quando permitido por lei. Funções podem mudar ou ser removidas. Faça backups independentes.

8. Limitação de responsabilidade
Na medida legalmente permitida, Keyt e colaboradores excluem danos indiretos, incidentais e consequentes, perda de tempo, dados, itens, receita e sanções decorrentes do uso ou impossibilidade de uso. Direitos obrigatórios do consumidor, fraude e responsabilidades não excluíveis permanecem. Aceitar não torna válida uma cláusula ilegal.

9. Licença
GPL-3.0, LICENSE e avisos de terceiros regem os componentes. Estes termos não restringem direitos GPL. Ao distribuir, preserve avisos e forneça o código-fonte correspondente exigido. Respeite direitos e atribuições das imagens; não alegue propriedade ou aprovação oficial.

10. Recusa e encerramento
Leia e aceite a cada abertura; consentimento anterior não é reutilizado. Se discordar, Fechar também interrompe captura. Reabrir exige consentimento e sincronização novos. Disponibilidade e resultados da conta não são garantidos.

11. Interpretação
Traduções ajudam a entender; proteções locais obrigatórias permanecem. Parte inválida não prejudica as demais válidas onde permitido. Não se inventam foro, lei aplicável ou endereço do operador. O rascunho precisa de revisão jurídica e linguística antes de distribuição pública.

12. Declaração
Marcar e ACEITAR confirma leitura, compreensão do risco de banimento permanente, continuação voluntária e capacidade de aceitar. Caso contrário, não prossiga. As regras da editora prevalecem e podem mudar.
""",
        ["ar"]="""
1. تطبيق مستقل
Aion 2 Pet Tracker أداة مجتمعية غير رسمية. لا يرتبط Keyt والمساهمون بشركة NC ولا يمثلون ناشر اللعبة ولا يدّعون موافقته. الأسماء والصور والعلامات ملك لأصحابها. هذه الشروط ليست إذناً رسمياً باستخدام برامج خارجية.

2. عقوبات الحساب وقبول المخاطر
قد يؤدي استخدام أدوات خارجية إلى التحقيق أو التقييد المؤقت أو الحظر الدائم وفقدان العناصر والمشتريات والوصول إلى الحساب. القراءة السلبية لا تضمن السماح أو الأمان أو عدم اكتشاف مكافحة الغش. بالمتابعة تقبل هذه المخاطر عن علم. لا يتحكم Keyt والمساهمون بقرارات الناشر، ولا يتحملون، إلى أقصى حد يسمح به القانون، مسؤولية الحظر أو القيود أو الخسائر الناتجة. لا يوجد وعد باستخدام خالٍ من الحظر.

3. الاستخدام القانوني
راجع شروط اللعبة وسياساتها الحالية قبل كل استخدام. توقف إذا كان الاستخدام ممنوعاً أو غير واضح. يجب أن تكون مخولاً لاستخدام الجهاز وقراءة الاتصال. لا تتجاوز الحماية أو تستخدم الغش أو تستغل الأخطاء أو تؤتمت اللعب أو تدخل حساب غيرك. لا تمنح هذه الشروط حق مخالفة القانون أو قواعد اللعبة.

4. التشغيل والمتطلبات
يقرأ الوضع الحي حركة الخادم إلى العميل المدعومة بشكل سلبي بواسطة Npcap المثبت بشكل منفصل. لا يرسل حزم اللعبة ولا يحقن فيها شيفرة ولا يولّد مدخلات اللعب. صلاحيات الإدارة تعتمد على تثبيت Npcap. تغييرات النظام أو الشبكة أو اللعبة قد تمنع العمل. لا تعطّل الحماية الأمنية.

5. دقة البيانات
العداد يحصي الأرواح المرصودة لا عمليات القتل. فقدان الحزم والانقطاع وتغير البروتوكول والمعرّفات غير المعروفة قد تسبب نقصاً أو عدم يقين. كل تشغيل يتطلب تعريفاً جديداً للشخصية ومجموعة متحققاً منها؛ القيم القديمة ليست حقيقة حالية. تحقق داخل اللعبة ولا تتخذ قرارات مالية أو غير قابلة للتراجع اعتماداً عليه.

6. البيانات المحلية والخصوصية
تُحفظ معرّفات الشخصية والخادم والعدادات والتفضيلات والسجلات محلياً في ملف مستخدم Windows. لا يُطلب منك كلمة مرور ولا تُرسل هذه السجلات إلى خدمة للمطور. الإصدار الحالي لا يكتب حزم الشبكة الخام. راجع السجلات وأخفِ المعلومات الشخصية قبل مشاركتها. لدى Npcap والبرامج الأخرى سياسات مستقلة. حذف مجلد البيانات بعد الإغلاق يمحو السجلات والتفضيلات.

7. عدم تقديم ضمانات
يُقدم البرنامج كما هو وحسب التوافر دون ضمان الدقة أو الاستمرارية أو التوافق أو الملاءمة التجارية أو لغرض معين أو الدعم المستمر، بالقدر المسموح قانوناً. قد تتغير الميزات أو تتوقف. احتفظ بنسخ احتياطية مستقلة للملفات المهمة.

8. حدود المسؤولية
إلى الحد القانوني، تُستبعد مسؤولية Keyt والمساهمين عن الضرر غير المباشر أو العرضي أو التبعي وفقدان الوقت والبيانات والعناصر والدخل والعقوبات المرتبطة بالاستخدام أو تعذره. لا تُستبعد حقوق المستهلك الإلزامية أو الاحتيال أو المسؤوليات التي لا يجوز استبعادها. الموافقة لا تجعل شرطاً غير قانوني قابلاً للتنفيذ.

9. الترخيص والتوزيع
تحكم GPL-3.0 وLICENSE وإشعارات الأطراف الثالثة المكونات ذات الصلة. لا تقيد هذه الشروط حقوق GPL. احتفظ بالإشعارات ووفّر المصدر الموافق عند التوزيع وفق الترخيص. احترم حقوق الصور ونسبتها؛ لا تدّع ملكيتها أو موافقة الناشر.

10. الرفض والإنهاء
اقرأ ووافق في كل تشغيل؛ لا تُعاد موافقة سابقة. إذا رفضت اختر إغلاق، وهو يوقف الالتقاط أيضاً. إعادة الفتح تتطلب موافقة ومزامنة جديدتين. لا ضمان لاستمرار التوافر أو نتائج الحساب.

11. التفسير القانوني
الترجمات تساعد الفهم ولا تلغي الحقوق المحلية الإلزامية. بطلان بند لا يلغي البنود الأخرى المشروعة حيث يسمح القانون. لم نختلق محكمة أو قانوناً حاكماً أو عنوان مشغّل. هذه مسودة تحتاج مراجعة قانونية ولغوية قبل التوزيع العام.

12. إقرارك
بتحديد المربع واختيار أوافق تؤكد القراءة وفهم احتمال الحظر الدائم والمتابعة طوعاً وامتلاك صلاحية القبول. إن لم تفهم المخاطر أو تقبلها فلا تتابع. قواعد الناشر هي المرجع وقد تتغير.
""",
        ["hi"]="""
1. स्वतंत्र ऐप
Aion 2 Pet Tracker एक अनौपचारिक सामुदायिक उपकरण है। Keyt और योगदानकर्ता NC से संबद्ध नहीं हैं, प्रकाशक का प्रतिनिधित्व या अनुमोदन दावा नहीं करते। नाम, चित्र और ट्रेडमार्क उनके मालिकों के हैं। ये शर्तें बाहरी सॉफ्टवेयर की आधिकारिक अनुमति नहीं हैं।

2. खाते की कार्रवाई और जोखिम स्वीकृति
बाहरी उपकरण से जाँच, अस्थायी रोक, स्थायी प्रतिबंध, वस्तुओं, खरीद और खाते तक पहुँच का नुकसान हो सकता है। निष्क्रिय रीडिंग अनुमति, सुरक्षा या एंटीचीट से अदृश्यता की गारंटी नहीं है। आगे बढ़कर आप ये जोखिम जानबूझकर स्वीकार करते हैं। Keyt और योगदानकर्ता प्रकाशक के निर्णय नियंत्रित नहीं करते और कानून द्वारा अनुमत अधिकतम सीमा में प्रतिबंध, खाते की रोक और संबंधित नुकसान के जिम्मेदार नहीं हैं। प्रतिबंध न होने का वादा नहीं है।

3. कानूनी उपयोग
हर उपयोग से पहले वर्तमान गेम नियम पढ़ें। निषिद्ध या अनिश्चित हो तो उपयोग बंद करें। उपकरण और कनेक्शन पढ़ने की अनुमति आपके पास होनी चाहिए। सुरक्षा न तोड़ें, धोखा, बग का दुरुपयोग, गेम ऑटोमेशन या दूसरे के खाते का उपयोग न करें। कानून या नियम तोड़ने का अधिकार नहीं मिलता।

4. संचालन और आवश्यकताएँ
लाइव मोड अलग स्थापित Npcap से समर्थित सर्वर से क्लाइंट ट्रैफिक निष्क्रिय रूप से पढ़ता है। गेम पैकेट नहीं भेजता, इंजेक्शन या गेम इनपुट नहीं बनाता। प्रशासनिक अनुमति Npcap पर निर्भर है। सिस्टम, नेटवर्क या गेम बदलाव कार्य रोक सकते हैं। सुरक्षा सुविधाएँ बंद न करें।

5. सटीकता
गिनती देखे गए सोल की है, हत्याओं की नहीं। पैकेट हानि, कनेक्शन टूटना, प्रोटोकॉल बदलाव या अज्ञात ID डेटा को अपूर्ण या अनिश्चित बना सकते हैं। हर शुरुआत में नई पात्र पहचान और सत्यापित संग्रह चाहिए। पुराने अंक वर्तमान सत्य नहीं हैं। महत्वपूर्ण संख्या गेम में जाँचें; वित्तीय या अपरिवर्तनीय फैसले इस उपकरण पर न आधारित करें।

6. स्थानीय डेटा और गोपनीयता
पात्र/सर्वर पहचान, काउंटर, पसंद और लॉग Windows उपयोगकर्ता प्रोफाइल में स्थानीय रहते हैं। पासवर्ड नहीं माँगा जाता और डेटा डेवलपर सेवा को नहीं भेजा जाता। वर्तमान संस्करण कच्चे नेटवर्क पैकेट नहीं लिखता। साझा करने से पहले लॉग में निजी जानकारी हटाएँ। Npcap और अन्य सॉफ्टवेयर की अलग नीतियाँ हैं। ऐप बंद करके डेटा फोल्डर हटाने से रिकॉर्ड और सेटिंग मिटेंगे।

7. कोई वारंटी नहीं
कानून द्वारा अनुमत सीमा में सॉफ्टवेयर जैसा है और उपलब्धता के अनुसार दिया जाता है; सटीकता, निरंतरता, संगतता, व्यापारिक योग्यता, विशेष उद्देश्य या लगातार सहायता की गारंटी नहीं। फीचर बदल या हट सकते हैं। महत्वपूर्ण फाइलों का स्वतंत्र बैकअप रखें।

8. जिम्मेदारी की सीमा
कानूनी सीमा में Keyt और योगदानकर्ता अप्रत्यक्ष, आकस्मिक या परिणामस्वरूप नुकसान, समय, डेटा, वस्तु, आय हानि और उपयोग या उपयोग न कर पाने से संबंधित कार्रवाई के उत्तरदायी नहीं। अनिवार्य उपभोक्ता अधिकार, धोखाधड़ी और ऐसी जिम्मेदारी जिसे कानूनन नहीं हटाया जा सकता, सुरक्षित हैं। सहमति गैरकानूनी प्रावधान को वैध नहीं बनाती।

9. लाइसेंस
GPL-3.0, LICENSE और तीसरे पक्ष के नोटिस संबंधित हिस्सों पर लागू हैं। ये शर्तें GPL अधिकार सीमित नहीं करतीं। वितरण में जरूरी नोटिस और संबंधित स्रोत कोड दें। चित्रों के अधिकार और श्रेय का सम्मान करें; अपना या आधिकारिक अनुमोदित न बताएँ।

10. अस्वीकृति और समाप्ति
हर लॉन्च में शर्तें फिर पढ़कर स्वीकार करें; पिछली सहमति दोबारा उपयोग नहीं होती। असहमत हों तो बंद करें, जिससे कैप्चर भी बंद होता है। पुनः खोलने पर नई सहमति और सिंक्रनाइज़ेशन चाहिए। उपलब्धता या खाते के परिणाम की गारंटी नहीं।

11. कानूनी अर्थ
अनुवाद समझने में मदद करते हैं और स्थानीय अनिवार्य अधिकार नहीं हटाते। अवैध भाग से बाकी वैध भाग, जहाँ कानून अनुमति दे, अप्रभावित हैं। अदालत, लागू कानून या संचालक का पता गढ़ा नहीं गया। सार्वजनिक वितरण से पहले मसौदे की कानूनी और भाषाई समीक्षा जरूरी है।

12. आपकी पुष्टि
बॉक्स चुनकर स्वीकार करें दबाने से आप पढ़ना, स्थायी प्रतिबंध का जोखिम समझना, स्वेच्छा से आगे बढ़ना और स्वीकारने का अधिकार पुष्टि करते हैं। जोखिम न समझें या स्वीकार न करें तो आगे न बढ़ें। प्रकाशक के नियम निर्णायक हैं और बदल सकते हैं।
""",
        ["zh"]="""
1. 独立应用
Aion 2 Pet Tracker 是非官方社区工具。Keyt 及贡献者与 NC 或 AION 2 发行商没有隶属关系，不代表发行商，也不声称获得批准。名称、肖像和商标属于各自权利人。本条款不是使用第三方软件的官方许可。

2. 账号处罚与明确接受风险
第三方工具可能导致账号调查、临时限制、永久封禁、道具或购买内容损失，以及失去账号访问权限。被动读取不保证获得许可、安全或不被反作弊检测。继续即表示您知情并接受这些风险。Keyt 及贡献者无法控制发行商决定；在适用法律允许的最大范围内，不对封号、账号限制及由此造成的损失负责。任何开发者都无法承诺永不封号。

3. 合法使用
每次使用前查看最新游戏协议和运营政策。禁止或不确定时请停止使用。您必须有权使用设备并读取所选连接。不得用于绕过保护、作弊、利用漏洞、自动化游戏或访问他人账号。本条款不授予违反法律或游戏规则的权利。

4. 工作方式与前提
实时模式通过另行安装的 Npcap 被动读取支持的服务器到客户端流量。不发送游戏数据包、不向游戏注入代码、不生成游戏输入。管理员权限取决于 Npcap 安装配置。系统、网络或游戏更新可能导致无法运行。请勿为使用本工具关闭安全保护。

5. 准确性与重新同步
计数表示观察到的灵魂获取，不是击杀数。丢包、断线、协议变化和不支持的 ID 可能导致缺失或不确定数据。每次启动必须重新识别角色并验证收藏；旧记录不视为当前事实。请在游戏中核实重要数字，不依赖本工具作出财务或不可逆决定。

6. 本地数据与隐私
角色和服务器标识、宠物计数、偏好及诊断日志保存在 Windows 用户资料目录。应用不索取密码，也不把这些记录发送到开发者服务。当前版本不写入原始网络数据包。日志仍可能含身份信息，请在分享前检查并去除。Npcap 及其他软件有自己的政策。关闭后删除应用数据文件夹将删除记录和偏好。

7. 无保证
在法律允许范围内，软件按现状及可用情况提供，不保证准确性、连续运行、兼容性、适销性、特定用途适用性或持续支持。更新可能修改或移除功能。请独立备份重要文件。

8. 责任限制
在合法范围内，Keyt 及贡献者不承担因使用或无法使用本应用产生的间接、附带、后果性损失，以及时间、数据、道具、收入损失和处罚责任。不排除法律禁止排除的责任、强制消费者权利、欺诈及不可放弃的义务。同意条款不代表所有免责内容都可依法执行。

9. 许可与分发
应用按 GPL-3.0 分发；LICENSE 与第三方声明适用于对应组件。本条款不限制 GPL 授予的权利。分发时请保留必要声明并按 GPL 提供相应源代码。宠物图片有独立署名和权利要求，不得宣称为自己所有或得到发行商认可。

10. 拒绝与终止
每次启动必须重新阅读并接受当前条款，不复用旧同意。不同意时选择关闭；关闭也停止捕获。再次启动需要重新同意及同步。不保证持续可用、游戏兼容或账号结果。

11. 法律解释
翻译帮助理解，不取消您所在地区的强制保护。如果某条款无效，在法律允许时其他合法条款仍有效。这里没有虚构管辖法院、适用法律或经营者地址。本草案在公开发行前需要针对地区的法律及语言审查。

12. 确认
勾选并点击接受，表示您已阅读、理解永久封号等风险、自愿继续并有权接受。如无法理解或接受风险，请勿继续。发行商规则具有决定性作用且可能改变。
"""
    };
    public static bool Complete=>UiText.Languages.All(l=>Bodies.TryGetValue(l.Code,out var body)&&body.Contains("12.")&&body.Length>700);
    public static string Get(string language)=>Version+"\n\n"+Bodies[language]+"\n\n"+PublisherPolicy;
}
