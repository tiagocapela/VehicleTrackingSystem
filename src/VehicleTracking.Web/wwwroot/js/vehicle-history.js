// src/VehicleTracking.Web/wwwroot/js/vehicle-history.js - Fixed for Route Line and Timeline

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
        this.animationSpeed = 1000;
    }

    initialize(locationsData) {
        console.log('Initializing history manager with locations:', locationsData);

        // Process the locations data safely
        this.locations = this.processLocationsData(locationsData);
        console.log('Processed locations:', this.locations.length);

        this.initializeMap();
        this.initializeSpeedChart();
        this.calculateStatistics();
        this.setupEventListeners();

        if (this.locations.length > 0) {
            console.log('Displaying route...');
            this.displayRoute();
            setTimeout(() => {
                this.fitMapToRoute();
            }, 500); // Small delay to ensure map is ready
        } else {
            console.warn('No location data available for display');
        }

        // Store instance globally for access from HTML
        window.historyManager = this;
    }

    processLocationsData(data) {
        if (!data || !Array.isArray(data)) {
            console.warn('Invalid locations data:', data);
            return [];
        }

        const processed = data.map(location => ({
            ...location,
            timestamp: this.parseDate(location.timestamp),
            speed: this.safeNumber(location.speed),
            course: this.safeNumber(location.course),
            satellites: this.safeNumber(location.satellites, 0),
            latitude: this.safeNumber(location.latitude),
            longitude: this.safeNumber(location.longitude)
        })).filter(location =>
            location.latitude !== 0 &&
            location.longitude !== 0 &&
            location.timestamp &&
            Math.abs(location.latitude) <= 90 &&
            Math.abs(location.longitude) <= 180
        ).sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp)); // Sort by timestamp

        console.log('Filtered and sorted locations:', processed.length);
        return processed;
    }

    parseDate(dateString) {
        if (!dateString) return null;
        const date = new Date(dateString);
        return isNaN(date.getTime()) ? null : date;
    }

    safeNumber(value, defaultValue = 0) {
        if (value === null || value === undefined || isNaN(value)) {
            return defaultValue;
        }
        return Number(value);
    }

    initializeMap() {
        const mapElement = document.getElementById('history-map');
        if (mapElement && !this.historyMap) {
            console.log('Initializing map...');
            this.historyMap = L.map('history-map', {
                zoomControl: true,
                scrollWheelZoom: true
            }).setView([39.7392, -104.9903], 10);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors',
                maxZoom: 18
            }).addTo(this.historyMap);

            console.log('Map initialized successfully');
        }
    }

    displayRoute() {
        if (!this.historyMap || this.locations.length === 0) {
            console.warn('Cannot display route - no map or no locations');
            return;
        }

        console.log('Displaying route with', this.locations.length, 'locations');

        // Clear existing route
        this.clearRoute();

        // Create route points array
        const routePoints = this.locations.map(loc => {
            console.log(`Point: [${loc.latitude}, ${loc.longitude}]`);
            return [loc.latitude, loc.longitude];
        });

        console.log('Route points:', routePoints);

        // Create the polyline with visible styling
        this.routePolyline = L.polyline(routePoints, {
            color: '#0d6efd',
            weight: 4,
            opacity: 0.8,
            smoothFactor: 1.0
        });

        // Add to map
        this.routePolyline.addTo(this.historyMap);
        console.log('Route polyline added to map');

        // Add start and end markers
        this.addStartEndMarkers();

        // Add stop markers
        this.addStopMarkers();

        // Force map update
        this.historyMap.invalidateSize();
    }

    addStartEndMarkers() {
        if (this.locations.length === 0) return;

        const startLocation = this.locations[0];
        const endLocation = this.locations[this.locations.length - 1];

        console.log('Adding start marker at:', startLocation.latitude, startLocation.longitude);
        console.log('Adding end marker at:', endLocation.latitude, endLocation.longitude);

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
        const stops = this.findStops();
        console.log('Found', stops.length, 'stops');

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
                    const duration = (location.timestamp - stopStart.timestamp) / (1000 * 60);
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

        console.log('Fitting map to route...');

        const bounds = L.latLngBounds(
            this.locations.map(loc => [loc.latitude, loc.longitude])
        );

        this.historyMap.fitBounds(bounds, {
            padding: [20, 20],
            maxZoom: 15
        });

        console.log('Map fitted to bounds:', bounds);
    }

    setupEventListeners() {
        console.log('Setting up event listeners...');

        // Timeline slider
        const slider = document.getElementById('timelineSlider');
        if (slider && this.locations.length > 0) {
            slider.max = this.locations.length - 1;
            slider.value = 0;

            // Remove existing event listeners
            slider.removeEventListener('input', this.handleSliderChange);
            slider.removeEventListener('change', this.handleSliderChange);

            // Bind the handler properly
            this.handleSliderChange = (e) => {
                const index = parseInt(e.target.value);
                console.log('Slider changed to index:', index);
                this.showLocationAtIndex(index);
            };

            slider.addEventListener('input', this.handleSliderChange);
            slider.addEventListener('change', this.handleSliderChange);

            console.log('Timeline slider configured with max:', slider.max);
        }
    }

    showLocationAtIndex(index) {
        if (index < 0 || index >= this.locations.length) {
            console.warn('Invalid index:', index);
            return;
        }

        const location = this.locations[index];
        this.currentAnimationIndex = index;

        console.log('Showing location at index:', index, location);

        // Update timeline info
        this.updateElement('current-time', this.formatDateTime(location.timestamp));
        this.updateElement('current-speed', `${location.speed || 0} km/h`);
        this.updateElement('current-position', `${location.latitude.toFixed(6)}, ${location.longitude.toFixed(6)}`);

        // Update map
        this.highlightLocation(location.id, location.latitude, location.longitude, location.speed || 0, location.timestamp);
    }

    highlightLocation(locationId, lat, lng, speed, timestamp) {
        if (!this.historyMap) return;

        console.log('Highlighting location:', lat, lng);

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
            `);

        // Center map on location with smooth pan
        this.historyMap.setView([lat, lng], Math.max(this.historyMap.getZoom(), 13), {
            animate: true,
            duration: 0.5
        });

        console.log('Map centered on:', lat, lng);

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
        const locationItem = document.querySelector(`[data-location-id="${locationId}"]`);
        if (locationItem) {
            locationItem.classList.add('bg-primary', 'text-white');
            locationItem.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }

    initializeSpeedChart() {
        const ctx = document.getElementById('speedChart');
        if (!ctx) return;

        if (this.locations.length === 0) {
            const parent = ctx.parentElement;
            parent.innerHTML = '<p class="text-muted text-center">No speed data available</p>';
            return;
        }

        // Prepare chart data - ensure timestamps are Date objects and data is sorted
        const chartData = this.locations
            .map(location => ({
                x: new Date(location.timestamp),
                y: location.speed || 0
            }))
            .sort((a, b) => a.x - b.x); // Sort by timestamp to ensure proper order

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
                    tension: 0.4,
                    pointRadius: 2,
                    pointHoverRadius: 4
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
                                minute: 'HH:mm',
                                hour: 'HH:mm',
                                day: 'MMM dd'
                            },
                            tooltipFormat: 'MMM dd, yyyy HH:mm:ss'
                        },
                        title: { 
                            display: true, 
                            text: 'Time' 
                        },
                        ticks: {
                            maxTicksLimit: 10
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
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            title: function(tooltipItems) {
                                if (tooltipItems.length > 0) {
                                    return new Date(tooltipItems[0].parsed.x).toLocaleString();
                                }
                                return '';
                            },
                            label: function(tooltipItem) {
                                return `Speed: ${tooltipItem.parsed.y} km/h`;
                            }
                        }
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

    calculateStatistics() {
        if (this.locations.length === 0) {
            this.updateElement('total-distance', '0.0 km');
            this.updateElement('avg-speed', '0 km/h');
            this.updateElement('max-speed', '0 km/h');
            return;
        }

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
        const speeds = this.locations
            .map(loc => loc.speed || 0)
            .filter(speed => speed > 0);

        const avgSpeed = speeds.length > 0 ? speeds.reduce((a, b) => a + b, 0) / speeds.length : 0;
        const maxSpeed = speeds.length > 0 ? Math.max(...speeds) : 0;

        // Update UI
        this.updateElement('total-distance', `${totalDistance.toFixed(1)} km`);
        this.updateElement('avg-speed', `${avgSpeed.toFixed(0)} km/h`);
        this.updateElement('max-speed', `${maxSpeed.toFixed(0)} km/h`);
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

    toggleAnimation() {
        if (this.locations.length === 0) {
            alert('No location data available for animation');
            return;
        }

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

        console.log('Starting animation with', this.locations.length, 'locations');

        this.isAnimating = true;
        this.currentAnimationIndex = 0;

        // Remove existing animation marker
        if (this.animationMarker) {
            this.historyMap.removeLayer(this.animationMarker);
        }

        // Create animation marker
        const firstLocation = this.locations[0];
        this.animationMarker = L.marker([firstLocation.latitude, firstLocation.longitude], {
            icon: this.createCustomIcon('blue', 'car')
        }).addTo(this.historyMap);

        // Start animation interval
        this.animationInterval = setInterval(() => {
            this.animateToNextPoint();
        }, this.animationSpeed);
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

        const location = this.locations[this.currentAnimationIndex];

        // Update marker position
        if (this.animationMarker) {
            this.animationMarker.setLatLng([location.latitude, location.longitude]);
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

        this.currentAnimationIndex++;
    }

    clearMap() {
        this.clearRoute();
        this.stopAnimation();
    }

    formatDateTime(dateTime) {
        if (!dateTime) return 'Invalid Date';
        if (!(dateTime instanceof Date)) {
            dateTime = new Date(dateTime);
        }
        return isNaN(dateTime.getTime()) ? 'Invalid Date' : dateTime.toLocaleString();
    }

    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }
}

// Global functions for HTML access
let historyManager = null;

function initializeHistoryPage() {
    console.log('Initializing history page...');

    historyManager = new VehicleHistoryManager();

    // Check if locationsData is available
    if (typeof locationsData !== 'undefined') {
        console.log('Using locationsData:', locationsData);
        historyManager.initialize(locationsData);
    } else {
        console.error('locationsData is not defined');
        historyManager.initialize([]);
    }

    // Store globally for HTML access
    window.historyManager = historyManager;
}

function highlightLocation(locationId, lat, lng) {
    if (historyManager && typeof locationsData !== 'undefined') {
        const location = locationsData.find(l => l.id === locationId);
        if (location) {
            historyManager.highlightLocation(locationId, lat, lng, location.speed || 0, location.timestamp);
        }
    }
}

function fitMapToRoute() {
    if (historyManager) {
        historyManager.fitMapToRoute();
    }
}

function toggleAnimation() {
    if (historyManager) {
        historyManager.toggleAnimation();
    }
}

function clearMap() {
    if (historyManager) {
        historyManager.clearMap();
    }
}

// Expose for debugging
window.debugHistoryManager = () => {
    console.log('History Manager Debug Info:');
    console.log('- Manager instance:', historyManager);
    console.log('- Locations count:', historyManager?.locations?.length || 0);
    console.log('- Map instance:', historyManager?.historyMap);
    console.log('- Route polyline:', historyManager?.routePolyline);
    console.log('- Current locations data:', historyManager?.locations);

    if (historyManager?.historyMap) {
        console.log('- Map center:', historyManager.historyMap.getCenter());
        console.log('- Map zoom:', historyManager.historyMap.getZoom());
    }
};