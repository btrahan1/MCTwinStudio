// Assets/behaviors.js
// This file acts as the "Standard Library" for your Digital Twins.
// It is injected into every exported cartridge.

window.MCTwinBehaviors = {

    // --- UTILITIES ---
    // Helper functions that any behavior can use

    // --- SIMPLE BEHAVIORS ---

    "Heartbeat": {
        // Simple pulsing animation
        // Args: Speed (default 5.0)
        onTick: (node, args, time) => {
            const speed = parseFloat(args.Speed || "5.0");
            const pulse = 1.0 + Math.sin(time * speed) * 0.2;
            node.scaling = new BABYLON.Vector3(pulse, pulse, pulse);
        }
    },

    "Spin": {
        // Continuous rotation on Y axis
        // Args: Speed (default 1.0)
        onTick: (node, args, time) => {
            const speed = parseFloat(args.Speed || "1.0");
            node.rotation.y += 0.01 * speed;
        }
    },

    "Door": {
        // Toggle rotation on click
        // Args: Angle (default 90)
        onInteract: (node, args) => {
            const isOpen = node.metadata.isOpen || false;
            const openAngle = BABYLON.Tools.ToRadians(parseFloat(args.Angle || "90"));
            const targetRotation = isOpen ? 0 : openAngle;

            BABYLON.Animation.CreateAndStartAnimation(
                "openDoor_" + node.id, node, "rotation.y", 30, 30,
                node.rotation.y, targetRotation, 0
            );

            node.metadata.isOpen = !isOpen;
        }
    },

    "SceneLink": {
        // Navigates to another URL
        // Args: Url, Target (_self, _blank)
        onInteract: (node, args) => {
            if (args.Url) {
                console.log("Navigating to: " + args.Url);
                window.open(args.Url, args.Target || "_self");
            }
        }
    },

    // --- DATA DRIVEN BEHAVIORS ---

    "Wave": {
        // "I want him to wave": Simulating a jump/wobble excitement animation
        // Args: Speed (default 10.0), Duration (default 2.0)
        onInteract: (node, args) => {
            // Start Waving
            node.metadata.isWaving = true;
            node.metadata.waveTimer = parseFloat(args.Duration || "2.0");
            node.metadata.startY = node.positio.y; // Capture baseline
        },
        onTick: (node, args, time) => {
            if (!node.metadata.isWaving) return;

            const speed = parseFloat(args.Speed || "10.0");
            const dt = 0.016; // Approx 60fps delta
            node.metadata.waveTimer -= dt;

            if (node.metadata.waveTimer <= 0) {
                // Stop Waving
                node.metadata.isWaving = false;
                node.position.y = node.metadata.startY;
                node.rotation.z = 0;
                return;
            }

            // 2. Jump (Y-Axis)
            node.position.y = node.metadata.startY + Math.abs(Math.sin(time * speed)) * 0.5;

            // 3. Wobble (Z-Axis Rotation)
            node.rotation.z = Math.sin(time * speed * 2) * 0.2;
        }
    },

    "MapLoader": {
        // Fetches locations and spawns pins
        // Args: ApiUrl (optional override)
        onInit: (node, args) => {
            console.log("MapLoader Initialized. Waiting for API...");
            // Mock API call for now - replace with fetch(args.ApiUrl)
            const mockLocations = [
                { id: "101", lat: 0, lon: 0 },
                { id: "102", lat: 10, lon: 5 },
                { id: "103", lat: -10, lon: -5 }
            ];

            mockLocations.forEach(loc => {
                // In a real map, convert Lat/Lon to Vector3
                const pos = new BABYLON.Vector3(loc.lat, 0, loc.lon);

                if (window.MCTwin && window.MCTwin.spawnRecipe) {
                    // Spawn a "Pin" (assuming Pin recipe exists)
                    // If no Pin recipe, we spawn a box
                    // We pass the BranchId to the pin so it knows where to link
                    /*
                    MCTwin.spawnRecipe("Pin", "Pin_" + loc.id, true, {
                        position: pos,
                        Tags: { 
                            Behavior: "SceneLink",
                            Url: `branch_viewer.html?branchId=${loc.id}`
                        }
                    });
                    */
                    console.log(`[Mock] Spawning Pin for Branch ${loc.id} at ${pos}`);
                }
            });
        }
    }
};

console.log("MCTwin Behaviors Loaded.");
