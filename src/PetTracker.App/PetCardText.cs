namespace PetTracker.App;
public static class PetCardText
{
    private static readonly Dictionary<string,string[]> Texts=new()
    {
        ["monsters"]=["Canavar / dungeon soul farmı","Monster / dungeon soul farming","Monster- / Dungeon-Seelenfarm","Фарм душ монстров / подземелий","Farmeo de almas de monstruos / mazmorras","Farm d’âmes de monstres / donjons","Farm de almas de monstros / masmorras","تجميع أرواح الوحوش / الزنزانات","मॉन्स्टर / डंजन सोल फ़ार्म","怪物 / 副本魂刷取"],
        ["sources"]=["Nasıl elde edilir?","How to obtain","So erhältst du es","Как получить","Cómo obtenerlo","Comment l’obtenir","Como obter","كيفية الحصول عليه","कैसे प्राप्त करें","获取方式"],
        ["field"]=["Haritada farm konumları var","Mapped farming locations","Farmorte auf der Karte","Места фарма на карте","Lugares de farmeo en el mapa","Lieux de farm sur la carte","Locais de farm no mapa","مواقع تجميع على الخريطة","मानचित्र पर फ़ार्म स्थान","有地图刷取位置"],
        ["special"]=["Canavar farmı yerine özel kaynaklar","Special sources instead of monster farming","Spezielle Quellen statt Monsterfarm","Особые источники вместо фарма","Fuentes especiales, no farmeo de monstruos","Sources spéciales, sans farm de monstres","Fontes especiais, sem farm de monstros","مصادر خاصة بدل تجميع الوحوش","मॉन्स्टर फ़ार्म के बजाय विशेष स्रोत","特殊来源，非怪物刷取"],
        ["unknown"]=["Farm yöntemi doğrulanmadı","Farming method unconfirmed","Farmmethode unbestätigt","Способ фарма не подтверждён","Método de farmeo sin confirmar","Méthode de farm non confirmée","Método de farm não confirmado","طريقة التجميع غير مؤكدة","फ़ार्म विधि अपुष्ट","刷取方式未确认"],
        ["missing"]=["Elde edilme kaynağı henüz doğrulanmadı. Konum bulunmaması, farmlanamaz olduğu anlamına gelmez.","Acquisition sources are not confirmed yet. Missing map coordinates do not mean this pet cannot be farmed.","Quellen noch unbestätigt. Fehlende Kartenorte bedeuten nicht, dass dieses Pet nicht farmbar ist.","Источники пока не подтверждены. Отсутствие координат не означает невозможность фарма.","Fuentes sin confirmar. La falta de coordenadas no significa que no pueda farmearse.","Sources non confirmées. L’absence de coordonnées ne signifie pas que le pet ne peut pas être farmé.","Fontes não confirmadas. A falta de coordenadas não significa que o pet não possa ser farmado.","المصادر غير مؤكدة. غياب الإحداثيات لا يعني أن الحيوان غير قابل للتجميع.","स्रोत अपुष्ट हैं। स्थान न होने का मतलब यह नहीं कि पेट फ़ार्म नहीं हो सकता।","获取来源尚未确认。没有地图坐标并不代表无法刷取。"],
        ["wiki"]=["Wiki kaynağını aç","Open wiki source","Wiki-Quelle öffnen","Открыть источник","Abrir fuente wiki","Ouvrir la source wiki","Abrir fonte wiki","فتح مصدر الويكي","विकी स्रोत खोलें","打开维基来源"],
        ["map"]=["Haritada göster","Show on map","Auf Karte zeigen","Показать на карте","Mostrar en mapa","Afficher sur la carte","Mostrar no mapa","عرض على الخريطة","मानचित्र पर दिखाएँ","在地图上显示"],
        ["last"]=["Son bilinen ilerleme","Last known progress","Letzter bekannter Stand","Последний известный прогресс","Último progreso conocido","Dernière progression connue","Último progresso conhecido","آخر تقدم معروف","अंतिम ज्ञात प्रगति","最后已知进度"],
        ["regions"]=["Kayıtlı bölgeler","Recorded regions","Gespeicherte Regionen","Записанные регионы","Regiones registradas","Régions enregistrées","Regiões registradas","المناطق المسجلة","दर्ज क्षेत्र","已记录区域"],
    };
    public static string Get(string lang,string key){var i=Array.FindIndex(UiText.Languages,l=>l.Code==lang);return Texts[key][i<0?1:i];}
}
