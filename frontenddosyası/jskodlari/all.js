// Harita div'ine erişim
const mapDiv = document.getElementById('map');

// Backend API URL'leri
const API_URLS = {
    LIVE: 'http://localhost:5000/api/Earthquake/get-earthquake-data',
    STORED: 'http://localhost:5000/api/Earthquake/get-stored-data'
};

// Veri kaynağı seçimi için değişken
let currentDataSource = 'LIVE'; // 'LIVE' veya 'STORED'

// Veri kaynağını değiştir
function toggleDataSource() {
    currentDataSource = currentDataSource === 'LIVE' ? 'STORED' : 'LIVE';
    const buttonText = document.getElementById('toggleDataButton');
    if (buttonText) {
        buttonText.textContent = currentDataSource === 'LIVE' ? 'MongoDB Verileri' : 'Canlı Veriler';
    }
    showEarthquakes();
}

// Deprem verilerini çekme fonksiyonu
async function fetchEarthquakeData() {
    const url = API_URLS[currentDataSource];
    try {
        console.log(`${currentDataSource} deprem verileri çekiliyor...`);
        const response = await fetch(url);
        
        if (!response.ok) {
            throw new Error(`HTTP hata! durum: ${response.status}`);
        }

        const data = await response.json();
        console.log('Çekilen veriler:', data);
        return data.data || {};
    } catch (error) {
        console.error('Deprem verisi çekilirken hata oluştu:', error);
        return {};
    }
}

// Depremleri haritada göster
async function showEarthquakes() {
    const earthquakeData = await fetchEarthquakeData();
    
    // Mevcut işaretçileri temizle
    map.eachLayer((layer) => {
        if (layer instanceof L.Circle) {
            map.removeLayer(layer);
        }
    });

    // Her şehir için deprem verilerini işle
    Object.entries(earthquakeData).forEach(([city, earthquakes]) => {
        earthquakes.forEach(quake => {
            if (quake.Coordinates && quake.Coordinates.length >= 2) {
                const magnitude = quake.Magnitude;
                let color = magnitude >= 6 ? 'red' : 
                           magnitude >= 4 ? 'orange' : 'yellow';

                const circle = L.circle([quake.Coordinates[1], quake.Coordinates[0]], {
                    color: color,
                    fillColor: color,
                    fillOpacity: 0.5,
                    radius: magnitude * 10000
                }).addTo(map);

                circle.bindPopup(`
                    <strong>${city}</strong><br>
                    Büyüklük: ${magnitude}<br>
                    Tarih: ${quake.Date}<br>
                    Saat: ${quake.Time}<br>
                    Derinlik: ${quake.Depth} km
                `);

                if (magnitude >= 6) {
                    triggerShakeAnimation();
                }
            }
        });
    });
}

// Sallanma animasyonu
function triggerShakeAnimation() {
    mapDiv.classList.add('shake');
    setTimeout(() => {
        mapDiv.classList.remove('shake');
    }, 500);
}

// Filtreleme panelini aç/kapat
function toggleFilter() {
    const filterPanel = document.getElementById('filterPanel');
    filterPanel.classList.toggle('open');
    document.getElementById('map').classList.toggle('panel-open');
}

// Filtreleme işlemi
async function applyFilter() {
    const location = document.getElementById('location').value.toLowerCase();
    const magnitudeRange = document.getElementById('magnitude-range').value;
    
    const earthquakeData = await fetchEarthquakeData();
    let filteredData = {};

    // Her şehir için filtreleme uygula
    Object.entries(earthquakeData).forEach(([city, earthquakes]) => {
        if (!location || city.toLowerCase().includes(location)) {
            const filteredEarthquakes = earthquakes.filter(quake => {
                const magnitude = quake.Magnitude;
                return magnitudeRange === 'all' ||
                    (magnitudeRange === 'low' && magnitude < 4) ||
                    (magnitudeRange === 'medium' && magnitude >= 4 && magnitude < 6) ||
                    (magnitudeRange === 'high' && magnitude >= 6);
            });

            if (filteredEarthquakes.length > 0) {
                filteredData[city] = filteredEarthquakes;
            }
        }
    });

    showFilteredEarthquakes(filteredData);
    toggleFilter();
}

// Filtrelenmiş depremleri göster
function showFilteredEarthquakes(filteredData) {
    // Mevcut işaretçileri temizle
    map.eachLayer((layer) => {
        if (layer instanceof L.Circle) {
            map.removeLayer(layer);
        }
    });

    // Filtrelenmiş verileri göster
    Object.entries(filteredData).forEach(([city, earthquakes]) => {
        earthquakes.forEach(quake => {
            if (quake.Coordinates && quake.Coordinates.length >= 2) {
                const magnitude = quake.Magnitude;
                let color = magnitude >= 6 ? 'red' : 
                           magnitude >= 4 ? 'orange' : 'yellow';

                L.circle([quake.Coordinates[1], quake.Coordinates[0]], {
                    color: color,
                    fillColor: color,
                    fillOpacity: 0.5,
                    radius: magnitude * 10000
                }).addTo(map)
                  .bindPopup(`
                    <strong>${city}</strong><br>
                    Büyüklük: ${magnitude}<br>
                    Tarih: ${quake.Date}<br>
                    Saat: ${quake.Time}<br>
                    Derinlik: ${quake.Depth} km
                  `);
            }
        });
    });
}

// Filtreleri sıfırla
function resetFilter() {
    document.getElementById('magnitude-range').value = 'all';
    document.getElementById('location').value = '';
    showEarthquakes();
    toggleFilter();
}

// Kullanıcı konumunu göster
function showUserLocation() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            position => {
                const userLat = position.coords.latitude;
                const userLon = position.coords.longitude;

                L.marker([userLat, userLon])
                    .addTo(map)
                    .bindPopup('Sizin Konumunuz')
                    .openPopup();

                map.setView([userLat, userLon], 10);
            },
            error => {
                console.error('Konum alınamadı:', error);
            }
        );
    } else {
        console.error('Tarayıcınız konum servislerini desteklemiyor.');
    }
}

// Haritayı başlat
const map = L.map('map').setView([39.9334, 32.8597], 6); // Türkiye koordinatları

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
}).addTo(map);

// Sayfa yüklendiğinde
document.addEventListener('DOMContentLoaded', () => {
    console.log('Sayfa yüklendi, veriler çekiliyor...');
    showEarthquakes();
    showUserLocation();
});
