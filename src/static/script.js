// Leaflet haritasını başlat (sadece bir kez)
var map = L.map('map').setView([40.78259, 29.94628], 13);

// Harita katmanları
var lightLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
});

var darkLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {});
darkLayer.addTo(map); // Varsayılan katman

// Katman kontrolü
L.control.layers({
    "Açık Mod": lightLayer,
    "Karanlık Mod": darkLayer
}).addTo(map);

// Marker'lar ve rotalar için global değişkenler
var startMarker = null, endMarker = null;
var currentRouteLayer = null;
var animatedPolyline = null;

// Durak ikonları
var busIcon = L.icon({
    iconUrl: '../static/assets/bus.png',
    iconSize: [32, 32],
    iconAnchor: [16, 32],
    popupAnchor: [0, -32]
});

var tramIcon = L.icon({
    iconUrl: '../static/assets/tram.png',
    iconSize: [32, 32],
    iconAnchor: [16, 32],
    popupAnchor: [0, -32]
});

// Durakları haritaya ekle (sadece bir kez)
function initializeMap() {
    fetch('../static/VeriSeti.json')
        .then(response => response.json())
        .then(data => addStopsToMap(data.duraklar))
        .catch(error => console.error('Durak verileri yüklenirken hata:', error));
}

function addStopsToMap(duraklar) {
    duraklar.forEach(stop => {
        const icon = stop.type === 'bus' ? busIcon : tramIcon;
        L.marker([stop.lat, stop.lon], { icon: icon })
            .addTo(map)
            .bindPopup(`<b>${stop.name}</b><br>Tip: ${stop.type}`);
    });
}

// Rota çizme fonksiyonu
function drawRouteWithWaypoints(routeData) {
    // Önceki rotayı temizle
    clearRoute();
    
    const firstRoute = routeData.result[0];
    const waypoints = firstRoute.path.map(point => [point.lat, point.lon]);
    const coordinates = waypoints.map(coord => `${coord[1]},${coord[0]}`).join(';');
    
    fetch(`https://router.project-osrm.org/route/v1/driving/${coordinates}?overview=full&geometries=geojson`)
        .then(response => response.json())
        .then(data => {
            const routeGeometry = data.routes[0].geometry;
            
            currentRouteLayer = L.geoJSON(routeGeometry, {
                style: { color: 'red', weight: 5, opacity: 0.7 }
            }).addTo(map);
            
            map.fitBounds(currentRouteLayer.getBounds());
            displayRouteDetails(routeData);
            setTimeout
        })
        .catch(error => console.error('Rota çizilirken hata:', error));
}

// Rota detaylarını göster
function displayRouteDetails(routeData) {
    const container = document.getElementById('routeDetailsContainer');
    container.innerHTML = '';
    
    // Ana rota bilgilerini göster
    document.getElementById('estimatedTime').innerText = `Tahmini Süre: ${routeData.total_time} dakika`;
    document.getElementById('estimatedCost').innerText = `Tahmini Maliyet: ${routeData.total_cost} TL`;
    document.getElementById('transfers').innerText = `Aktarmalar: ${routeData.transfer_count}`;
    
    // Detaylı rota bilgileri
    routeData.result.forEach((route, index) => {
        const routeInfo = document.createElement('div');
        routeInfo.className = 'route-info';
        routeInfo.innerHTML = `
            <h3>Bölüm ${index + 1}: ${route.type}</h3>
            <p><strong>Mesafe:</strong> ${route.distance} km</p>
            <p><strong>Süre:</strong> ${route.time} dakika</p>
            <p><strong>Ücret:</strong> ${route.price} TL</p>
        `;
        container.appendChild(routeInfo);
    });
}

// Haritayı temizle
function clearRoute() {
    if (currentRouteLayer) {
        map.removeLayer(currentRouteLayer);
        currentRouteLayer = null;
    }
    if (animatedPolyline) {
        map.removeLayer(animatedPolyline);
        animatedPolyline = null;
    }
}

// Reset fonksiyonu
function resetMap() {
    clearRoute();
    if (startMarker) map.removeLayer(startMarker);
    if (endMarker) map.removeLayer(endMarker);
    startMarker = endMarker = null;
    
    document.getElementById('currentLocation').value = '';
    document.getElementById('destination').value = '';
    document.getElementById('routeDetailsContainer').innerHTML = '';
    
    document.getElementById('estimatedTime').innerText = 'Tahmini Süre: -';
    document.getElementById('estimatedCost').innerText = 'Tahmini Maliyet: -';
    document.getElementById('transfers').innerText = 'Aktarmalar: -';
}

// Haritaya tıklama olayı
map.on('click', function(e) {
    const latlng = e.latlng;
    
    if (!startMarker) {
        startMarker = L.marker(latlng).addTo(map);
        document.getElementById('currentLocation').value = `${latlng.lat}, ${latlng.lng}`;
    } else if (!endMarker) {
        endMarker = L.marker(latlng).addTo(map);
        document.getElementById('destination').value = `${latlng.lat}, ${latlng.lng}`;
    }
});

// Buton event listener'ları
document.getElementById('resetButton').addEventListener('click', resetMap);

document.getElementById('routeButton').addEventListener('click',async function(event) {
    event.preventDefault();
    event.stopPropagation()
    const currentLocation = document.getElementById('currentLocation').value;
    const destination = document.getElementById('destination').value;
    
    if (!currentLocation || !destination) {
        alert('Lütfen başlangıç ve bitiş noktalarını seçin!');
        return;
    }
    
    const startCoords = currentLocation.split(',').map(coord => parseFloat(coord.trim()));
    const endCoords = destination.split(',').map(coord => parseFloat(coord.trim()));
    
    // Butonu devre dışı bırak
    const button = this;
    button.disabled = true;
    button.textContent = 'Rota Oluşturuluyor...';
    
    fetch("http://localhost:5000/bus", {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            start_lat: startCoords[0],
            start_lon: startCoords[1],
            end_lat: endCoords[0],
            end_lon: endCoords[1],
            kullanici_tipi: "ogrenci",
        }),
        credentials: 'omit'
    })
    .then(response => {
        if (response.redirected) {
            console.warn("API yönlendirme yapıyor!");
            return response.text(); // Örnek amaçlı
        }
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.json();
    })
    .then(data => {
        drawRouteWithWaypoints(data);
        return data; // Rota verilerini döndür
    })
    .catch(error => {
        console.error('Error:', error);
        alert('Rota oluşturulurken hata: ' + error.message);
    })
    .finally(() => {
        button.disabled = false;
        button.textContent = 'Rota Oluştur';
    });
});

// Sayfa yüklendiğinde haritayı başlat
document.addEventListener('DOMContentLoaded', initializeMap);