// ================================================================================================
// src/VehicleTracking.Web/wwwroot/js/vehicle-tracking.js
// ================================================================================================
class VehicleTrackingManager {
    constructor() {
        this.dashboardMap = null;
        this.liveMap = null;
        this.trackingMap = null;
        this.vehicles = [];
        this.vehicleMarkers = {};
        this.currentVehicle = null;
        this.trackingInterval = null;
        this.signalRConnection = null;
        this.autoRefresh = false;
        this.refreshInterval = null;
    }

    async initialize() {
        await this.initializeSignalR();
        await this.loadVehicles();
    }

    async initializeSignalR() {
        try {
            this.signalRConnection = new signalR.HubConnectionBuilder()
                .withUrl("/vehicleTrackingHub")
                .build();

            await this.signalRConnection.start();
            console.log("SignalR Connected");

            this.signalRConnection.on("LocationUpdated", (vehicleId, location) => {
                const parsedLocation = {
                    ...location,
                    timestamp: new Date(location.timestamp),
                    speed: this.safeNumber(location.speed),
                    course: this.safeNumber(location.course),
                    satellites: this.safeNumber(location.satellites, 0)
                };
                this.handleLocationUpdate(vehicleId, parsedLocation);
            });

        } catch (err) {
            console.error("SignalR Connection Error: ", err);
        }
    }

    async loadVehicles() {
        try {
            const response = await fetch('/api/gps/vehicles');
            const data = await response.json();
            
            this.vehicles = data.map(vehicle => ({
                ...vehicle,
                createdAt: new Date(vehicle.createdAt),
                lastLocation: vehicle.lastLocation ? {
                    ...vehicle.lastLocation,
                    timestamp: new Date(vehicle.lastLocation.timestamp),
                    speed: this.safeNumber(vehicle.lastLocation.speed),
                    course: this.safeNumber(vehicle.lastLocation.course),
                    satellites: this.safeNumber(vehicle.lastLocation.satellites, 0)
                } : null
            }));
            
            this.updateDashboard();
            this.updateVehicleList();
            this.updateMapMarkers();
        } catch (error) {
            console.error('Error loading vehicles:', error);
        }
    }

    safeNumber(value, defaultValue = 0) {
        if (value === null || value === undefined || isNaN(value)) {
            return defaultValue;
        }
        return Number(value);
    }

    initializeDashboard() {
        if (document.getElementById('dashboard-map')) {
            this.dashboardMap = L.map('dashboard-map').setView([39.7392, -104.9903], 10);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors'
            }).addTo(this.dashboardMap);
        }
    }

    initializeLiveMap() {
        if (document.getElementById('live-map')) {
            this.liveMap = L.map('live-map').setView([39.7392, -104.9903], 10);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors'
            }).addTo(this.liveMap);
        }
    }

    initializeVehicleTracking(vehicleId, vehicleData) {
        this.currentVehicle = vehicleData;
        
        if (document.getElementById('tracking-map')) {
            this.trackingMap = L.map('tracking-map').setView([39.7392, -104.9903], 15);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors'
            }).addTo(this.trackingMap);

            this.loadVehicleLocation(vehicleId);
            this.loadRecentLocations(vehicleId);
        }
    }

    updateDashboard() {
        const total = this.vehicles.length;
        const online = this.vehicles.filter(v => this.isVehicleOnline(v)).length;
        const offline = total - online;
        const moving = this.vehicles.filter(v => this.isVehicleMoving(v)).length;

        this.updateElement('total-vehicles', total);
        this.updateElement('online-vehicles', online);
        this.updateElement('offline-vehicles', offline);
        this.updateElement('moving-vehicles', moving);
    }

    updateVehicleList() {
        const listContainer = document.getElementById('vehicle-list');
        if (!listContainer) return;

        listContainer.innerHTML = '';

        this.vehicles.forEach(vehicle => {
            const isOnline = this.isVehicleOnline(vehicle);
            const lastLocation = vehicle.lastLocation;
            
            const vehicleItem = document.createElement('div');
            vehicleItem.className = 'mb-3 p-2 border rounded cursor-pointer';
            vehicleItem.innerHTML = `
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <strong>${vehicle.vehicleName}</strong>
                        <br>
                        <small class="text-muted">${vehicle.licensePlate || 'No plate'}</small>
                    </div>
                    <div class="text-end">
                        <span class="badge ${isOnline ? 'bg-success' : 'bg-secondary'}">
                            ${isOnline ? 'Online' : 'Offline'}
                        </span>
                        <br>
                        <small class="text-muted">
                            ${lastLocation ? this.formatDate(lastLocation.timestamp) : 'No data'}
                        </small>
                    </div>
                </div>
            `;
            
            vehicleItem.addEventListener('click', () => {
                window.location.href = `/Vehicle/Track/${vehicle.id}`;
            });
            
            listContainer.appendChild(vehicleItem);
        });
    }

    updateMapMarkers() {
        if (!this.dashboardMap && !this.liveMap) return;

        const map = this.dashboardMap || this.liveMap;
        
        Object.values(this.vehicleMarkers).forEach(marker => {
            map.removeLayer(marker);
        });
        this.vehicleMarkers = {};

        this.vehicles.forEach(vehicle => {
            if (vehicle.lastLocation) {
                const isOnline = this.isVehicleOnline(vehicle);
                
                const marker = L.marker([vehicle.lastLocation.latitude, vehicle.lastLocation.longitude])
                    .bindPopup(`
                        <div class="text-center">
                            <strong>${vehicle.vehicleName}</strong><br>
                            ${vehicle.licensePlate || 'No plate'}<br>
                            <small>Speed: ${vehicle.lastLocation.speed || 0} km/h</small><br>
                            <small>${this.formatDate(vehicle.lastLocation.timestamp)}</small><br>
                            <button class="btn btn-sm btn-primary mt-1" onclick="window.location.href='/Vehicle/Track/${vehicle.id}'">
                                Track Vehicle
                            </button>
                        </div>
                    `);
                
                marker.addTo(map);
                this.vehicleMarkers[vehicle.id] = marker;
            }
        });
    }

    async loadVehicleLocation(vehicleId) {
        try {
            const response = await fetch(`/api/gps/vehicle/${vehicleId}/latest`);
            if (response.ok) {
                const locationData = await response.json();
                
                const location = {
                    ...locationData,
                    timestamp: new Date(locationData.timestamp),
                    speed: this.safeNumber(locationData.speed),
                    course: this.safeNumber(locationData.course),
                    satellites: this.safeNumber(locationData.satellites, 0)
                };
                
                this.updateLocationDetails(location);
                this.centerMapOnLocation(location);
                this.addVehicleMarker(location);
            }
        } catch (error) {
            console.error('Error loading vehicle location:', error);
        }
    }

    async loadRecentLocations(vehicleId) {
        try {
            const to = new Date();
            const from = new Date(to.getTime() - 24 * 60 * 60 * 1000);
            
            const response = await fetch(`/api/gps/vehicle/${vehicleId}/history?from=${from.toISOString()}&to=${to.toISOString()}`);
            const locationsData = await response.json();
            
            const locations = locationsData.map(loc => ({
                ...loc,
                timestamp: new Date(loc.timestamp),
                speed: this.safeNumber(loc.speed),
                course: this.safeNumber(loc.course),
                satellites: this.safeNumber(loc.satellites, 0)
            }));
            
            const container = document.getElementById('recent-locations');
            if (!container) return;
            
            container.innerHTML = '';
            
            if (locations.length === 0) {
                container.innerHTML = '<p class="text-muted">No recent locations</p>';
                return;
            }
            
            locations.slice(-10).reverse().forEach(location => {
                const item = document.createElement('div');
                item.className = 'mb-2 p-2 bg-light rounded cursor-pointer';
                item.innerHTML = `
                    <div class="d-flex justify-content-between">
                        <small><strong>${this.formatTime(location.timestamp)}</strong></small>
                        <small>${location.speed} km/h</small>
                    </div>
                    <small class="text-muted">
                        ${location.latitude.toFixed(4)}, ${location.longitude.toFixed(4)}
                    </small>
                `;
                
                item.addEventListener('click', () => {
                    this.centerMapOnLocation(location);
                    this.updateLocationDetails(location);
                });
                
                container.appendChild(item);
            });
        } catch (error) {
            console.error('Error loading recent locations:', error);
        }
    }

    centerMapOnLocation(location) {
        if (this.trackingMap) {
            this.trackingMap.setView([location.latitude, location.longitude], 15);
        } else if (this.liveMap) {
            this.liveMap.setView([location.latitude, location.longitude], 15);
        }
    }

    addVehicleMarker(location) {
        if (!this.trackingMap) return;

        if (window.currentTrackingMarker) {
            this.trackingMap.removeLayer(window.currentTrackingMarker);
        }
        
        window.currentTrackingMarker = L.marker([location.latitude, location.longitude])
            .addTo(this.trackingMap)
            .bindPopup(`
                <strong>${this.currentVehicle?.vehicleName || 'Vehicle'}</strong><br>
                Speed: ${location.speed || 0} km/h<br>
                <small>${this.formatDate(location.timestamp)}</small>
            `);
    }

    updateLocationDetails(location) {
        const container = document.getElementById('location-details');
        if (!container) return;

        const lat = Number(location.latitude).toFixed(6);
        const lng = Number(location.longitude).toFixed(6);
        const speed = this.safeNumber(location.speed).toFixed(0);
        const course = this.safeNumber(location.course).toFixed(0);
        const satellites = this.safeNumber(location.satellites, 0);

        container.innerHTML = `
            <div class="row">
                <div class="col-6">
                    <small class="text-muted">Latitude</small>
                    <div>${lat}</div>
                </div>
                <div class="col-6">
                    <small class="text-muted">Longitude</small>
                    <div>${lng}</div>
                </div>
            </div>
            <div class="row mt-2">
                <div class="col-6">
                    <small class="text-muted">Speed</small>
                    <div>${speed} km/h</div>
                </div>
                <div class="col-6">
                    <small class="text-muted">Course</small>
                    <div>${course}°</div>
                </div>
            </div>
            <div class="row mt-2">
                <div class="col-6">
                    <small class="text-muted">Satellites</small>
                    <div>${satellites}</div>
                </div>
                <div class="col-6">
                    <small class="text-muted">Last Update</small>
                    <div>${this.formatDate(location.timestamp)}</div>
                </div>
            </div>
        `;
    }

    toggleTracking() {
        const button = document.getElementById('tracking-btn-text');
        if (!button) return;

        if (this.trackingInterval) {
            clearInterval(this.trackingInterval);
            this.trackingInterval = null;
            button.textContent = 'Start';
            button.previousElementSibling.className = 'fas fa-play';
        } else {
            if (!this.currentVehicle) {
                alert('Please select a vehicle to track');
                return;
            }
            
            this.trackingInterval = setInterval(() => {
                this.loadVehicleLocation(this.currentVehicle.id);
            }, 10000);
            
            button.textContent = 'Stop';
            button.previousElementSibling.className = 'fas fa-stop';
        }
    }

    centerOnVehicle() {
        if (this.currentVehicle && this.currentVehicle.lastLocation) {
            this.centerMapOnLocation(this.currentVehicle.lastLocation);
        }
    }

    handleLocationUpdate(vehicleId, location) {
        const vehicle = this.vehicles.find(v => v.id === vehicleId);
        if (vehicle) {
            vehicle.lastLocation = location;
        }

        if (this.currentVehicle && this.currentVehicle.id === vehicleId) {
            this.updateLocationDetails(location);
            this.addVehicleMarker(location);
        }

        this.updateDashboard();
        this.updateMapMarkers();
    }

    isVehicleOnline(vehicle) {
        if (!vehicle.lastLocation) return false;
        const lastUpdate = new Date(vehicle.lastLocation.timestamp);
        const now = new Date();
        const diffMinutes = (now - lastUpdate) / (1000 * 60);
        return diffMinutes < 15;
    }

    isVehicleMoving(vehicle) {
        if (!vehicle.lastLocation) return false;
        return (vehicle.lastLocation.speed || 0) > 5;
    }

    formatDate(date) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        return isNaN(date.getTime()) ? 'Invalid Date' : date.toLocaleString();
    }

    formatTime(date) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        return isNaN(date.getTime()) ? 'Invalid Time' : date.toLocaleTimeString();
    }

    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }
}

// Global instance
const vehicleTracker = new VehicleTrackingManager();

// Global functions
function initializeDashboard() {
    vehicleTracker.initializeDashboard();
    vehicleTracker.initialize();
}

function initializeLiveMap() {
    vehicleTracker.initializeLiveMap();
    vehicleTracker.initialize();
}

function initializeVehicleTracking(vehicleId, vehicleData) {
    vehicleTracker.initializeVehicleTracking(vehicleId, vehicleData);
    vehicleTracker.initialize();
}

function toggleTracking() {
    vehicleTracker.toggleTracking();
}

function centerOnVehicle() {
    vehicleTracker.centerOnVehicle();
}

// Auto-refresh data every 30 seconds
setInterval(() => {
    vehicleTracker.loadVehicles();
}, 30000);