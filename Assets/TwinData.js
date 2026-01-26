// Assets/TwinData.js

/**
 * TwinData
 * The central brain for "State". 
 * Visualizers ask THIS object for data, they never talk to APIs directly.
 */
window.TwinData = {
    // The current state of the world
    _store: {
        "Global": { "Time": "12:00" },
        "Store_01": {
            "Sales": 1500,
            "Target": 2000,
            "ATM": { "CashLevel": 75, "Status": "OK" }
        }
    },

    /**
     * RETRIEVE: The strategy for getting data.
     * In the future, this is where we put `fetch('https://api.mybank.com')`
     */
    refresh: async function () {
        console.log("TwinData: Refreshing from Source...");

        // --- SIMULATION (Delete this when connecting to real API) ---
        // We simulate values changing so you can see the twin react
        this._store.Store_01.Sales += Math.floor(Math.random() * 100);
        if (this._store.Store_01.Sales > 3000) this._store.Store_01.Sales = 0;

        this._store.Store_01.ATM.CashLevel = Math.floor(Math.random() * 100);
        // -----------------------------------------------------------

        return this._store;
    },

    /**
     * ACCESS: How visualizers find what they need.
     * path: "Store_01.ATM.CashLevel"
     */
    get: function (pathString) {
        return pathString.split('.').reduce((obj, key) => (obj && obj[key] !== 'undefined') ? obj[key] : null, this._store);
    }
};
