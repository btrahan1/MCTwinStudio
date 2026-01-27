window.initMap = async function (dotNetRef, canvasId, locations) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    if (canvas._engine) canvas._engine.dispose();

    const engine = new BABYLON.Engine(canvas, true);
    canvas._engine = engine;

    const scene = new BABYLON.Scene(engine);
    scene.clearColor = new BABYLON.Color4(0.01, 0.01, 0.02, 1);

    // Camera: Centered on US
    // Center of US is roughly Lon -96, Lat 37.
    const camera = new BABYLON.ArcRotateCamera("camera",
        BABYLON.Tools.ToRadians(270),
        BABYLON.Tools.ToRadians(30),
        60, // Distance to see everything
        new BABYLON.Vector3(0, 0, 0),
        scene
    );
    camera.attachControl(canvas, true);

    const light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(0, 1, 0), scene);
    light.intensity = 0.6;

    // Glow Layer for that "Digital" look
    const glow = new BABYLON.GlowLayer("glow", scene);
    glow.intensity = 0.4;

    const guiTexture = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("UI", true, scene);

    // --- Data Processing Constants ---
    // Centering the US at (0,0,0) helps orbit controls work better.
    const centerLon = -96.0;
    const centerLat = 37.0;
    const globalScale = 1.0;

    // Helper: Convert GPS to World Vector3
    function gpsToVector(lon, lat) {
        return new BABYLON.Vector3(
            (lon - centerLon) * globalScale,
            0,
            (lat - centerLat) * globalScale
        );
    }

    // --- ONE: Load and Build the Map Logic ---
    try {
        const response = await fetch('data/us-states.json');
        const data = await response.json();

        const stateMat = new BABYLON.StandardMaterial("stateMat", scene);
        stateMat.diffuseColor = new BABYLON.Color3(0.05, 0.05, 0.1);
        stateMat.emissiveColor = new BABYLON.Color3(0, 0.2, 0.4); // Dark Blue Glow
        stateMat.alpha = 0.8;
        stateMat.backFaceCulling = false;

        const borderMat = new BABYLON.StandardMaterial("borderMat", scene);
        borderMat.emissiveColor = new BABYLON.Color3(0.0, 0.5, 1.0); // Bright Blue Borders

        data.features.forEach(feature => {
            const geometry = feature.geometry;
            if (geometry.type === "Polygon" || geometry.type === "MultiPolygon") {

                const coordinates = geometry.type === "Polygon" ? [geometry.coordinates] : geometry.coordinates;

                coordinates.forEach(polygon => {
                    const shape = [];
                    // GeoJSON format: [Lon, Lat]
                    // Outer ring is usually the first element
                    const ring = polygon[0];

                    ring.forEach(coord => {
                        const pt = gpsToVector(coord[0], coord[1]);
                        shape.push(new BABYLON.Vector3(pt.x, 0, pt.z));
                    });

                    // Extrude State Shape
                    const stateMesh = BABYLON.MeshBuilder.ExtrudePolygon("state_" + feature.id, {
                        shape: shape,
                        depth: 0.2,
                        sideOrientation: BABYLON.Mesh.DOUBLESIDE
                    }, scene, earcut);

                    stateMesh.position.y = 0.2;
                    stateMesh.material = stateMat;

                    // Optional: Draw Borders (Lines)
                    const points = shape.map(v => new BABYLON.Vector3(v.x, 0.21, v.z));
                    // Close the loop
                    points.push(points[0]);
                    const lines = BABYLON.MeshBuilder.CreateLines("lines_" + feature.id, { points: points }, scene);
                    lines.color = new BABYLON.Color3(0.2, 0.6, 1.0);
                });
            }
        });

    } catch (err) {
        console.error("Failed to parse map data", err);
    }

    // --- TWO: Plot Store Locations ---
    const pylonMat = new BABYLON.StandardMaterial("pylonMat", scene);
    pylonMat.emissiveColor = new BABYLON.Color3(0.0, 1.0, 0.5); // Green Pylons
    pylonMat.alpha = 1.0;

    locations.forEach(loc => {
        const pos = gpsToVector(loc.longitude, loc.latitude);

        const pylon = BABYLON.MeshBuilder.CreateCylinder("pylon_" + loc.name, {
            height: 2,
            diameterTop: 0.1,
            diameterBottom: 0.4,
            tessellation: 12
        }, scene);

        pylon.position = pos;
        pylon.position.y = 1; // Base at 0, height 2 -> Center at 1
        pylon.material = pylonMat;

        // Label
        const cleanName = loc.name.split('(')[0].replace('Store ', '').trim();
        const label = new BABYLON.GUI.TextBlock();
        label.text = cleanName;
        label.color = "white";
        label.fontSize = 14;
        label.fontWeight = "bold";
        label.outlineWidth = 2;
        label.outlineColor = "black";

        guiTexture.addControl(label);
        label.linkWithMesh(pylon);
        label.linkOffsetY = -40;
    });

    engine.runRenderLoop(() => {
        scene.render();
    });

    window.addEventListener("resize", () => {
        engine.resize();
    });
};

window.triggerMapResize = function () {
    window.dispatchEvent(new Event('resize'));
};
