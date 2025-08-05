// src/VehicleTracking.Web / wwwroot / js / json - utils.js - Utility functions for JSON handling
// Create this separate file for reusable JSON utilities

window.JsonUtils = window.JsonUtils || {};

// Safe JSON parsing with error handling
window.JsonUtils.safeParse = function (jsonString, defaultValue = null) {
    try {
        return JSON.parse(jsonString);
    } catch (error) {
        console.error('JSON Parse Error:', error, 'Data:', jsonString);
        return defaultValue;
    }
};

// Convert C# DateTime strings to JavaScript Date objects
window.JsonUtils.parseDotNetDate = function (dateString) {
    if (!dateString) return null;

    // Handle various .NET DateTime formats
    const date = new Date(dateString);
    return isNaN(date.getTime()) ? null : date;
};

// Safe number conversion
window.JsonUtils.toNumber = function (value, defaultValue = 0) {
    if (value === null || value === undefined) return defaultValue;
    const num = Number(value);
    return isNaN(num) ? defaultValue : num;
};

// Format numbers consistently
window.JsonUtils.formatNumber = function (value, decimals = 2) {
    const num = window.JsonUtils.toNumber(value);
    return num.toFixed(decimals);
};

// Process vehicle data from server
window.JsonUtils.processVehicleData = function (vehicleData) {
    return {
        ...vehicleData,
        createdAt: window.JsonUtils.parseDotNetDate(vehicleData.createdAt),
        lastLocation: vehicleData.lastLocation ? window.JsonUtils.processLocationData(vehicleData.lastLocation) : null
    };
};

// Process location data from server
window.JsonUtils.processLocationData = function (locationData) {
    return {
        ...locationData,
        timestamp: window.JsonUtils.parseDotNetDate(locationData.timestamp),
        speed: window.JsonUtils.toNumber(locationData.speed),
        course: window.JsonUtils.toNumber(locationData.course),
        satellites: window.JsonUtils.toNumber(locationData.satellites, 0),
        latitude: window.JsonUtils.toNumber(locationData.latitude),
        longitude: window.JsonUtils.toNumber(locationData.longitude)
    };
};

// Process array of locations
window.JsonUtils.processLocationsArray = function (locationsArray) {
    return (locationsArray || []).map(location => window.JsonUtils.processLocationData(location));
};

// Debug function to inspect JSON data
window.JsonUtils.inspect = function (data, label = 'Data') {
    console.group(label);
    console.log('Type:', typeof data);
    console.log('Value:', data);
    if (typeof data === 'object' && data !== null) {
        console.log('Keys:', Object.keys(data));
        if (Array.isArray(data)) {
            console.log('Array length:', data.length);
            if (data.length > 0) {
                console.log('First item:', data[0]);
            }
        }
    }
    console.groupEnd();
};