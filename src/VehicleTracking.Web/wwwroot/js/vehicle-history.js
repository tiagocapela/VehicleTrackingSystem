// src/VehicleTracking.Web/wwwroot/js/vehicle-history.js

class VehicleHistoryManager {
    constructor() {
        this.historyMap = null;
        this.routePolyline = null;
        this.locationMarkers = [];
        this.currentLocationMarker = null;
        this.animationMarker = null;
        this.speedChart = null;
        this.isAnimating = false;
        this.animationInterval = null;
        this.currentAnimationIndex = 0;
        this.locations = [];
    }

    initialize(locationsData) {
        this.locations = locationsData || [];
        this.initializeMap();
        this.initializeSpeedChart();
        this.calculateStatistics();
        this.setupEventListeners();

        if (this.locations.length > 0) {
            this.displayRoute();
            this.fitMapToRoute();
        }

        // Store instance globally for access from HTML
        window.historyManager = this;
    }

    initializeMap() {
        const mapElement = document.getElementById('history-map');
        if (mapElement) {
            this.historyMap = L.map('history-map').setView([39.7392, -104.9903], 10);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors'
            }).addTo(this.historyMap);
        }
    }

    initializeSpeedChart() {
        const ctx = document.getElementById('speedChart');
        if (!ctx) return;

        if (this.locations.length === 0) {
            // Show empty chart message
            const parent = ctx.parentElement;
            parent.innerHTML = '<p class="text-muted text-center">No speed data available</p>';
            return;
        }

        const chartData = this.locations.map(location => ({
            x: new Date(location.timestamp),
            y: location.speed || 0
        }));

        this.speedChart = new Chart(ctx, {
            type: 'line',
            data: {
                datasets: [{
                    label: 'Speed (km/h)',
                    data: chartData,
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: {
                        type: 'time',
                        time: {
                            displayFormats: {
                                hour: 'HH:mm',
                                day: 'MMM DD'
                            }
                        }
                    },
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Speed (km/h)'
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                },
                interaction: {
                    intersect: false,
                    mode: 'index'
                },
                onHover: (event, activeElements) => {
                    if (activeElements.length > 0) {
                        const index = activeElements[0].index;
                        this.highlightLocationByIndex(index);
                    }
                }
            }
        });
    }

    displayRoute() {
        if (!this.historyMap || this.locations.length === 0) return;

        // Clear existing route
        this.clearRoute();

        // Create route polyline
        const routePoints = this.locations.map(loc => [loc.latitude, loc.longitude]);

        this.routePolyline = L.polyline(routePoints, {
            color: '#0d6efd',
            weight: 4,
            opacity: 0.8
        }).addTo(this.historyMap);

        // Add start and end markers
        this.addStartEndMarkers();

        // Add stop markers
        this.addStopMarkers();
    }

    addStartEndMarkers() {
        if (this.locations.length === 0) return;

        const startLocation = this.locations[0];
        const endLocation = this.locations[this.locations.length - 1];

        // Start marker (green)
        const startMarker = L.marker([startLocation.latitude, startLocation.longitude], {
            icon: this.createCustomIcon('green', 'play')
        }).addTo(this.historyMap)
            .bindPopup(`
              <div class="text-center">
                  <strong>Journey Start</strong><br>
                  ${this.formatDateTime(startLocation.timestamp)}<br>
                  Speed: ${startLocation.speed || 0} km/h
              </div>
          `);

        this.locationMarkers.push(startMarker);

        // End marker (red) - only if different from start
        if (this.locations.length > 1) {
            const endMarker = L.marker([endLocation.latitude, endLocation.longitude], {
                icon: this.createCustomIcon('red', 'stop')
            }).addTo(this.historyMap)
                .bindPopup(`
                  <div class="text-center">
                      <strong>Journey End</strong><br>
                      ${this.formatDateTime(endLocation.timestamp)}<br>
                      Speed: ${endLocation.speed || 0} km/h
                  </div>
              `);

            this.locationMarkers.push(endMarker);
        }
    }

    addStopMarkers() {
        // Find stops (speed = 0 for more than 5 minutes)
        const stops = this.findStops();

        stops.forEach(stop => {
            const marker = L.marker([stop.latitude, stop.longitude], {
                icon: this.createCustomIcon('orange', 'pause')
            }).addTo(this.historyMap)
                .bindPopup(`
                  <div class="text-center">
                      <strong>Stop</strong><br>
                      ${this.formatDateTime(stop.timestamp)}<br>
                      Duration: ${stop.duration} minutes
                  </div>
              `);

            this.locationMarkers.push(marker);
        });
    }

    findStops() {
        const stops = [];
        let stopStart = null;

        for (let i = 0; i < this.locations.length; i++) {
            const location = this.locations[i];
            const speed = location.speed || 0;

            if (speed < 5) { // Consider as stopped if speed < 5 km/h
                if (!stopStart) {
                    stopStart = location;
                }
            } else {
                if (stopStart) {
                    const duration = (new Date(location.timestamp) - new Date(stopStart.timestamp)) / (1000 * 60);
                    if (duration > 5) { // Only consider stops longer than 5 minutes
                        stops.push({
                            ...stopStart,
                            duration: Math.round(duration)
                        });
                    }
                    stopStart = null;
                }
            }
        }

        return stops;
    }

    createCustomIcon(color, iconName) {
        return L.divIcon({
            html: `<div style="background-color: ${color}; width: 20px; height: 20px; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white; font-size: 10px; border: 2px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.2);"><i class="fas fa-${iconName}"></i></div>`,
            iconSize: [20, 20],
            className: 'custom-div-icon'
        });
    }

    clearRoute() {
        if (this.routePolyline) {
            this.historyMap.removeLayer(this.routePolyline);
            this.routePolyline = null;
        }

        this.locationMarkers.forEach(marker => {
            this.historyMap.removeLayer(marker);
        });
        this.locationMarkers = [];

        if (this.animationMarker) {
            this.historyMap.removeLayer(this.animationMarker);
            this.animationMarker = null;
        }

        if (this.currentLocationMarker) {
            this.historyMap.removeLayer(this.currentLocationMarker);
            this.currentLocationMarker = null;
        }
    }

    fitMapToRoute() {
        if (!this.historyMap || this.locations.length === 0) return;

        const bounds = L.latLngBounds(
            this.locations.$values.map(loc => [loc.latitude, loc.longitude])
        );

        this.historyMap.fitBounds(bounds, { padding: [20, 20] });
    }

    calculateStatistics() {
        if (this.locations.length === 0) return;

        // Calculate total distance
        let totalDistance = 0;
        for (let i = 1; i < this.locations.length; i++) {
            const prev = this.locations[i - 1];
            const curr = this.locations[i];
            totalDistance += this.calculateDistance(
                prev.latitude, prev.longitude,
                curr.latitude, curr.longitude
            );
        }

        // Calculate speeds
        const speeds = this.locations.map(loc => loc.speed || 0);
        const avgSpeed = speeds.reduce((a, b) => a + b, 0) / speeds.length;
        const maxSpeed = Math.max(...speeds);

        // Update UI
        this.updateElement('total-distance', `${totalDistance.toFixed(1)} km`);
        this.updateElement('avg-speed', `${avgSpeed.toFixed(0)} km/h`);
        this.updateElement('max-speed', `${maxSpeed.toFixed(0)} km/h`);

        // Load detailed statistics from API
        this.loadDetailedStatistics();
    }

    async loadDetailedStatistics() {
        try {
            const urlParams = new URLSearchParams(window.location.search);
            const from = urlParams.get('from') || new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString();
            const to = urlParams.get('to') || new Date().toISOString();

            const response = await fetch(`/api/gps/vehicle/${vehicleId}/statistics?from=${from}&to=${to}`);
            if (response.ok) {
                const stats = await response.json();

                this.updateElement('total-distance', `${stats.totalDistance} km`);
                this.updateElement('avg-speed', `${stats.averageSpeed} km/h`);
                this.updateElement('max-speed', `${stats.maxSpeed} km/h`);
            }
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }

    calculateDistance(lat1, lon1, lat2, lon2) {
        const R = 6371; // Earth's radius in kilometers
        const dLat = this.degreesToRadians(lat2 - lat1);
        const dLon = this.degreesToRadians(lon2 - lon1);
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(this.degreesToRadians(lat1)) * Math.cos(this.degreesToRadians(lat2)) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return R * c;
    }

    degreesToRadians(degrees) {
        return degrees * (Math.PI / 180);
    }

    setupEventListeners() {
        // Timeline slider
        const slider = document.getElementById('timelineSlider');
        if (slider && this.locations.length > 0) {
            slider.max = this.locations.length - 1;
            slider.addEventListener('input', (e) => {
                const index = parseInt(e.target.value);
                this.showLocationAtIndex(index);
            });
        }
    }

    showLocationAtIndex(index) {
        if (index < 0 || index >= this.locations.length) return;

        const location = this.locations[index];
        this.currentAnimationIndex = index;

        // Update timeline info
        this.updateElement('current-time', this.formatDateTime(location.timestamp));
        this.updateElement('current-speed', `${location.speed || 0} km/h`);
        this.updateElement('current-position', `${location.latitude.toFixed(6)}, ${location.longitude.toFixed(6)}`);

        // Update map
        this.highlightLocation(location.id, location.latitude, location.longitude, location.speed || 0, location.timestamp);
    }

    highlightLocation(locationId, lat, lng, speed, timestamp) {
        if (!this.historyMap) return;

        // Remove existing highlight marker
        if (this.currentLocationMarker) {
            this.historyMap.removeLayer(this.currentLocationMarker);
        }

        // Add new highlight marker
        this.currentLocationMarker = L.marker([lat, lng], {
            icon: this.createCustomIcon('purple', 'crosshairs')
        }).addTo(this.historyMap)
            .bindPopup(`
              <div class="text-center">
                  <strong>Selected Location</strong><br>
                  ${this.formatDateTime(timestamp)}<br>
                  Speed: ${speed} km/h<br>
                  Position: ${lat.toFixed(6)}, ${lng.toFixed(6)}
              </div>
          `).openPopup();

        // Center map on location
        this.historyMap.setView([lat, lng], Math.max(this.historyMap.getZoom(), 15));

        // Highlight in location list
        this.highlightLocationInList(locationId);
    }

    highlightLocationByIndex(index) {
        if (index >= 0 && index < this.locations.length) {
            const location = this.locations[index];
            this.highlightLocation(location.id, location.latitude, location.longitude, location.speed || 0, location.timestamp);

            // Update timeline slider
            const slider = document.getElementById('timelineSlider');
            if (slider) {
                slider.value = index;
            }
        }
    }

    highlightLocationInList(locationId) {
        // Remove previous highlights
        document.querySelectorAll('.location-item').forEach(item => {
            item.classList.remove('bg-primary', 'text-white');
        });

        // Add highlight to selected item
        const locationItem = document.querySelector(`.location-item[data-location-id="${locationId}"]`);
        if (locationItem) {
            locationItem.classList.add('bg-primary', 'text-white');
            locationItem.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }

    toggleAnimation() {
        const buttonElement = document.querySelector('#animation-btn-text');
        const iconElement = buttonElement?.previousElementSibling;

        if (!buttonElement) return;

        if (this.isAnimating) {
            this.stopAnimation();
            buttonElement.textContent = 'Play';
            if (iconElement) iconElement.className = 'fas fa-play me-1';
        } else {
            this.startAnimation();
            buttonElement.textContent = 'Pause';
            if (iconElement) iconElement.className = 'fas fa-pause me-1';
        }
    }

    startAnimation() {
        if (this.locations.length === 0) return;

        this.isAnimating = true;
        this.currentAnimationIndex = 0;

        // Remove existing animation marker
        if (this.animationMarker) {
            this.historyMap.removeLayer(this.animationMarker);
        }

        // Create animation marker
        this.animationMarker = L.marker([this.locations.$values[0].latitude, this.locations.$values[0].longitude], {
            icon: this.createCustomIcon('blue', 'car')
        }).addTo(this.historyMap);

        // Start animation interval
        this.animationInterval = setInterval(() => {
            this.animateToNextPoint();
        }, 1000); // 1 second between points
    }

    stopAnimation() {
        this.isAnimating = false;
        if (this.animationInterval) {
            clearInterval(this.animationInterval);
            this.animationInterval = null;
        }
    }

    animateToNextPoint() {
        if (this.currentAnimationIndex >= this.locations.length - 1) {
            this.stopAnimation();
            const buttonElement = document.querySelector('#animation-btn-text');
            const iconElement = buttonElement?.previousElementSibling;
            if (buttonElement) {
                buttonElement.textContent = 'Replay';
                if (iconElement) iconElement.className = 'fas fa-redo me-1';
            }
            return;
        }

        this.currentAnimationIndex++;
        const location = this.locations[this.currentAnimationIndex];

        // Update marker position
        if (this.animationMarker) {
            this.animationMarker.setLatLng([location.latitude, location.longitude]);
            this.animationMarker.setPopupContent(`
                <div class="text-center">
                    <strong>Animating...</strong><br>
                    ${this.formatDateTime(location.timestamp)}<br>
                    Speed: ${location.speed || 0} km/h
                </div>
            `);
        }

        // Update timeline
        const slider = document.getElementById('timelineSlider');
        if (slider) {
            slider.value = this.currentAnimationIndex;
        }

        // Update current info
        this.updateElement('current-time', this.formatDateTime(location.timestamp));
        this.updateElement('current-speed', `${location.speed || 0} km/h`);
        this.updateElement('current-position', `${location.latitude.toFixed(6)}, ${location.longitude.toFixed(6)}`);

        // Follow the marker
        this.historyMap.panTo([location.latitude, location.longitude]);
    }

    clearMap() {
        this.clearRoute();
    }

    formatDateTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString();
    }

    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }
}

// Create global instance
const historyManager = new VehicleHistoryManager();

// Global functions
function initializeHistoryPage() {
    historyManager.initialize(locationsData);
}

function highlightLocation(locationId, lat, lng) {
    // Find the location data to get additional info
    const location = locationsData.find(l => l.id === locationId);
    if (location) {
        historyManager.highlightLocation(locationId, lat, lng, location.speed, location.timestamp);
    }
}

function fitMapToRoute() {
    historyManager.fitMapToRoute();
}

function toggleAnimation() {
    historyManager.toggleAnimation();
}

function clearMap() {
    historyManager.clearMap();
}

function setQuickRange(range) {
    const now = new Date();
    let from, to = now;

    switch (range) {
        case 'today':
            from = new Date(now.getFullYear(), now.getMonth(), now.getDate());
            break;
        case 'yesterday':
            from = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1);
            to = new Date(now.getFullYear(), now.getMonth(), now.getDate());
            break;
        case 'week':
            from = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
            break;
        case 'month':
            from = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
            break;
        default:
            return;
    }

    // Update form inputs
    document.getElementById('fromDate').value = from.toISOString().slice(0, 16);
    document.getElementById('toDate').value = to.toISOString().slice(0, 16);

    // Submit form
    document.getElementById('historyFilterForm').submit();
}

function exportHistory() {
    const urlParams = new URLSearchParams(window.location.search);
    const from = urlParams.get('from') || new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString();
    const to = urlParams.get('to') || new Date().toISOString();

    const exportUrl = `/Vehicle/ExportHistory/${vehicleId}?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&format=csv`;
    window.open(exportUrl, '_blank');
}