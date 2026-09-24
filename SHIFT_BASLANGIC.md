# SHIFT — Kütle Protokolü

Unity 6000.3.24f1 · Windows · İki bölümlük, geliştirilebilir öğrenci portfolyosu prototipi.

## Oynamak

Windows oyunu `Builds/SHIFT-0.1/SHIFT.exe` konumuna üretilir. EXE'yi aynı klasördeki veri klasörü ve DLL'lerden ayırmayın. Menüde Türkçe/English seçimi bulunur.

- **A / D veya yön okları:** yürü.
- **Space:** zıpla.
- **E:** servis istasyonunda ağırlık değiştir; ağır modda pistonun üstünde mekanizmayı kilitle.
- **R:** bulunduğun bölümdeki tüm mekanizmaları ve robotu sıfırla.
- **Esc:** duraklat, devam et, dili değiştir veya oyundan çık.

Hafif modda yüksek basamaklara çıkılır. Ağır modda sarı çizgili panel, kısa bir uyarının ardından kırılır. Pistonlar bir kez çalışınca açık kalır. Düşünce son kullanılan istasyona dönülür; mekanizma ilerlemesi korunur.

## Unity'de açmak

1. Unity Hub'dan **PortfolioLab** projesini aç.
2. Project panelinde **Assets → Scenes → 05_ShiftFirstShift** sahnesini aç.
3. Üstte Play'e bas, sonra oyunun içindeki Başla düğmesini kullan.
4. İkinci sahne **06_ShiftPrepareTheWay**. İlk bölüm tamamlanınca oyun buraya geçer.

Değişiklik yapmak için önce Play'i kapat. Play sırasında Inspector'da yaptığın değer değişiklikleri genellikle oyun durduğunda geri alınır.

## İlk bölüm

S0'da hafifleş → yüksek basamaklardan S1'e çık → ağırlaş → F1'i kırarak alt odaya in → P1'i ağır modda kilitle → S2'de hafifleş → sağdaki basamaklarla çıkışa git.

## İkinci bölüm

Planlı rota: S0'da hafifleş → üst bakım cebindeki S1'e çık → ağırlaşarak P1 ile köprüyü aç → hafifleşerek bakım cebinden çık → merkez S0'da ağırlaş → F2'den alt odaya in → P2'yi kilitle → S2'de hafifleş → köprüyü geç, çıkışa tırman.

Önce aşağı inersen: P2'yi kilitle; soldaki K servis kapısı da açılır. S2'de hafifleşip soldaki servis merdivenlerinden merkeze dön. Üst P1'i tamamladıktan sonra açık F2 boşluğundan aşağı dönerek çıkış yolunu kullan. P2 ilerlemesi korunur.

## Geliştirmek için nereden başlamalı?

Hierarchy'deki gruplar:

- **01 ENVIRONMENT:** fiziksel platformlar ve duvarlar. Platformları taşıyarak bölüm düzenini değiştir.
- **02 MECHANISMS:** istasyonlar, paneller, pistonlar, kapılar, köprü, çıkış. Piston üzerindeki `Gates` / `Bridges` listeleri hangi yolları açtığını belirler.
- **03 ROBOT:** `ShiftRobot` bileşeninde hareket ve iki zıplama değeri. Görünüş çocuk nesnelerde; fiziksel gövde sabittir.
- **04 BACKDROP:** çarpışması olmayan duvarlar, borular, yazılar ve ışık görselleri.
- **05 ROOM RULES:** bölüm akışı, çıkış hedefi, mekanizma listeleri, sonraki sahne.

`Assets/Scripts/ShiftGame` içindeki her bileşenin ayrı işi var: hareket, görsel animasyon, istasyon, kırılan zemin, piston, kapı, köprü, kamera ve bölüm yönetimi. `Assets/ShiftPrototype/Materials` renkleri, `Assets/ShiftPrototype/Prefabs/ShiftRobot` robot başlangıç şablonunu içerir.

Yeni bölüm eklerken mevcut sahnenin kopyasını farklı isimle kaydet; platformları ve bağlantıları düzenle, Room Rules listesini ve sonraki sahneyi güncelle. Mevcut bileşenler yeni yerleşimlerde tekrar kullanılabilir.

**Önemli:** `Portfolio → SHIFT → Generate two robot levels` başlangıç sahnelerini yeniden üretir ve 05/06 üzerine yazar. Kendi değişikliklerini kaybetmemek için sahnelerini farklı isimle kaydet. Normal geliştirmede bu menüyü tekrar çalıştırman gerekmez. `Build Windows game` ise mevcut 05/06 sahnelerinden oyun çıktısı üretir.

## Portfolyo geliştirme kaydı

Bu sürüm, ağır/hafif taslağının ilk uygulamasıdır. Kod, başlangıç bölüm geometrisi ve geometrik robot asistan desteğiyle hazırlanmıştır. Kuzey'in yaptığı yerleşim değişiklikleri, ayar seçimleri ve oyuncu testlerinden çıkardığı sonuçlar ayrıca kaydedilmelidir.

Gerçek hedef oyuncu testi ve okulun sunum dosyası henüz tamamlanmadı. İlk testlerde özellikle şunları kaydet:

1. Oyuncu ağırlığın yalnız istasyonda değiştiğini ne zaman anlıyor?
2. P1'in köprüyü açtığını fark ediyor mu?
3. İkinci bölümde erken inişten sonra K dönüş yolunu buluyor mu?
4. Hangi atlayışlarda kontrol veya köşe takılması sorunu yaşıyor?

İlk denemede bölüm süresi ve yardım ihtiyacını ölç. Sonra tek bir yerleşim veya ipucu değiştirip yeniden dene; bulguları uydurulmuş başarı puanlarıyla değiştirme.

## Önceki çalışmalar

01_Blockout, 02_MovementLab ve 03/04 mıknatıs prototipi ayrı tutuldu. GitHub deposu bu önceki öğrenme çalışmalarını da içerir.
