window.initMap = async function (dotNetRef, canvasId, locations) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    if (canvas._engine) canvas._engine.dispose();

    const engine = new BABYLON.Engine(canvas, true);
    canvas._engine = engine;

    const scene = new BABYLON.Scene(engine);
    scene.clearColor = new BABYLON.Color4(0.01, 0.01, 0.02, 1);

    // Global State
    let isZoomed = false;
    let mapRoot = new BABYLON.TransformNode("mapRoot", scene); // Parent for Map Elements
    let detailRoot = new BABYLON.TransformNode("detailRoot", scene); // Parent for Detail Model
    detailRoot.setEnabled(false); // Hidden by default
    let recyclerCassettes = [];


    let camera = new BABYLON.ArcRotateCamera("camera",
        BABYLON.Tools.ToRadians(270),
        BABYLON.Tools.ToRadians(30),
        60,
        new BABYLON.Vector3(0, 0, 0),
        scene
    );
    camera.attachControl(canvas, true);
    camera.lowerRadiusLimit = 5; // Allow close Zoom

    const light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(0, 1, 0), scene);
    light.intensity = 0.6;
    const dirLight = new BABYLON.DirectionalLight("dirLight", new BABYLON.Vector3(-1, -2, -1), scene);
    dirLight.intensity = 0.6;

    const glow = new BABYLON.GlowLayer("glow", scene);
    glow.intensity = 0.4;

    const guiTexture = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("UI", true, scene);

    const centerLon = -96.0;
    const centerLat = 37.0;
    const globalScale = 1.0;

    function gpsToVector(lon, lat) {
        return new BABYLON.Vector3(
            (lon - centerLon) * globalScale,
            0,
            (lat - centerLat) * globalScale
        );
    }

    // --- 1. Map Geometry (Map Root) ---
    try {
        const response = await fetch('data/us-states.json');
        const data = await response.json();

        const stateMat = new BABYLON.StandardMaterial("stateMat", scene);
        stateMat.diffuseColor = new BABYLON.Color3(0.05, 0.05, 0.1);
        stateMat.emissiveColor = new BABYLON.Color3(0, 0.2, 0.4);
        stateMat.alpha = 0.8;
        stateMat.backFaceCulling = false;

        data.features.forEach(feature => {
            const geometry = feature.geometry;
            if (geometry.type === "Polygon" || geometry.type === "MultiPolygon") {
                const coordinates = geometry.type === "Polygon" ? [geometry.coordinates] : geometry.coordinates;
                coordinates.forEach(polygon => {
                    const shape = [];
                    const ring = polygon[0];
                    ring.forEach(coord => {
                        const pt = gpsToVector(coord[0], coord[1]);
                        shape.push(new BABYLON.Vector3(pt.x, 0, pt.z));
                    });
                    const stateMesh = BABYLON.MeshBuilder.ExtrudePolygon("state_" + feature.id, {
                        shape: shape,
                        depth: 0.2,
                        sideOrientation: BABYLON.Mesh.DOUBLESIDE
                    }, scene, earcut);
                    stateMesh.position.y = 0.2;
                    stateMesh.material = stateMat;
                    stateMesh.parent = mapRoot; // Attach to Map Root

                    const points = shape.map(v => new BABYLON.Vector3(v.x, 0.21, v.z));
                    points.push(points[0]);
                    const lines = BABYLON.MeshBuilder.CreateLines("lines_" + feature.id, { points: points }, scene);
                    lines.color = new BABYLON.Color3(0.2, 0.6, 1.0);
                    lines.parent = mapRoot; // Attach to Map Root
                });
            }
        });
    } catch (err) { console.error(err); }

    // --- 2. Store Pylons (Map Root) ---
    const pylonMat = new BABYLON.StandardMaterial("pylonMat", scene);
    pylonMat.emissiveColor = new BABYLON.Color3(0.0, 1.0, 0.5);
    pylonMat.alpha = 1.0;

    let pylonMap = {}; // Map ID to Mesh for lookup

    locations.forEach(loc => {
        const pos = gpsToVector(loc.longitude, loc.latitude);
        const pylon = BABYLON.MeshBuilder.CreateCylinder("pylon_" + loc.name, {
            height: 2, diameterTop: 0.1, diameterBottom: 0.4, tessellation: 12
        }, scene);
        pylon.position = pos;
        pylon.position.y = 1;
        pylon.material = pylonMat;
        pylon.parent = mapRoot;
        pylon.metadata = { id: loc.locationId }; // Store ID for click

        pylonMap[loc.locationId] = pylon; // Save reference

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

        // Hide/Show label based on mapRoot visibility
        scene.onBeforeRenderObservable.add(() => {
            label.isVisible = mapRoot.isEnabled();
        });
    });

    // --- 3. Interaction Logic ---
    scene.onPointerDown = (evt) => {
        if (isZoomed) return; // Prevent clicking while zoomed in
        const pick = scene.pick(scene.pointerX, scene.pointerY);
        if (pick.hit && pick.pickedMesh && pick.pickedMesh.metadata) {
            const locId = pick.pickedMesh.metadata.id;
            if (locId) {
                dotNetRef.invokeMethodAsync("SelectLocation", locId);
            }
        }
    };

    // --- 4. Drill-Down Functions (Exposed to DotNet) ---

    // Create Detailed Model (Reuse Logic from Ref)
    function createRecyclerDetail() {
        // Clear previous Labels explicitly
        recyclerCassettes.forEach(mesh => {
            if (mesh.guiLabel) mesh.guiLabel.dispose();
        });

        // Clear previous Meshes
        detailRoot.dispose();
        recyclerCassettes = [];
        detailRoot = new BABYLON.TransformNode("detailRoot", scene);

        const bodyMat = new BABYLON.StandardMaterial("bodyMat", scene);
        bodyMat.diffuseColor = new BABYLON.Color3(0.15, 0.15, 0.2);

        // Chassis (Taller to fit 6)
        const chassis = BABYLON.MeshBuilder.CreateBox("chassis", { width: 1.6, height: 4.0, depth: 2.2 }, scene);
        chassis.parent = detailRoot;
        chassis.position.y = 2.0;
        chassis.material = bodyMat;

        // Sidecar (Taller)
        const sidecar = BABYLON.MeshBuilder.CreateBox("sidecar", { width: 1.2, height: 3.5, depth: 2.0 }, scene);
        sidecar.parent = detailRoot;
        sidecar.position.set(1.4, 1.75, 0);
        sidecar.material = bodyMat;

        // Cassettes (Visual Representation)
        const cassMat = new BABYLON.StandardMaterial("cassMat", scene);
        cassMat.emissiveColor = new BABYLON.Color3(0, 0.8, 0.5);

        // Support up to 6 Cassettes (1, 5, 10, 20, 50, 100)
        for (let i = 0; i < 6; i++) {
            const cassette = BABYLON.MeshBuilder.CreateBox(`cassette_${i}`, { width: 1.2, height: 0.4, depth: 1.6 }, scene);
            cassette.parent = detailRoot;
            // Spacing: Start at 0.5, step by 0.6
            cassette.position.set(0, 0.5 + (i * 0.6), 0);
            cassette.material = cassMat;

            // Cassette Label
            const label = new BABYLON.GUI.TextBlock();
            label.text = "Loading..."; // Default
            label.color = "#00ff88";
            label.fontSize = 18; // Slightly smaller to fit
            label.fontWeight = "bold";
            guiTexture.addControl(label);
            label.linkWithMesh(cassette);
            label.linkOffsetX = 120;

            cassette.guiLabel = label;
            recyclerCassettes.push(cassette);

            // Visibility check
            scene.onBeforeRenderObservable.add(() => {
                label.isVisible = detailRoot.isEnabled();
            });
        }
    }

    window.updateDetailData = function (cassetteData) {
        if (!cassetteData || cassetteData.length === 0) return;
        cassetteData.forEach((c, i) => {
            if (i < recyclerCassettes.length) {
                const mesh = recyclerCassettes[i];
                if (mesh.guiLabel) {
                    const denom = c.denomination || c.Denomination || "?";
                    const count = c.currentCount || c.CurrentCount || 0;
                    mesh.guiLabel.text = `$${denom}: ${count}`;
                }
            }
        });
    };


    // Fly To Animation
    window.flyToLocation = function (id) {
        if (isZoomed) return;
        const targetPylon = pylonMap[id];
        if (!targetPylon) return;

        createRecyclerDetail(); // Rebuild/Reset model

        isZoomed = true;

        // Move Detail Model to Target Position
        detailRoot.position.set(targetPylon.position.x, 0, targetPylon.position.z);

        // Scene Swap: Hide Map, Show Detail
        mapRoot.setEnabled(false);
        detailRoot.setEnabled(true);

        const targetPos = detailRoot.position.add(new BABYLON.Vector3(0, 1.5, 0));

        // Animation
        const fps = 60;
        const duration = 1.0;
        const totalFrames = fps * duration;

        const animTarget = new BABYLON.Animation("animT", "target", fps, BABYLON.Animation.ANIMATIONTYPE_VECTOR3, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        animTarget.setKeys([{ frame: 0, value: camera.target.clone() }, { frame: totalFrames, value: targetPos }]);

        const animRadius = new BABYLON.Animation("animR", "radius", fps, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        animRadius.setKeys([{ frame: 0, value: camera.radius }, { frame: totalFrames, value: 8 }]);

        scene.beginDirectAnimation(camera, [animTarget, animRadius], 0, totalFrames, false);
    };

    // Fly Back Animation
    window.flyBackToMap = function () {
        if (!isZoomed) return;
        isZoomed = false;

        detailRoot.setEnabled(false);
        mapRoot.setEnabled(true);

        const targetPos = new BABYLON.Vector3(0, 0, 0); // Back to Center

        const fps = 60;
        const duration = 1.0;
        const totalFrames = fps * duration;

        const animTarget = new BABYLON.Animation("animT", "target", fps, BABYLON.Animation.ANIMATIONTYPE_VECTOR3, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        animTarget.setKeys([{ frame: 0, value: camera.target.clone() }, { frame: totalFrames, value: targetPos }]);

        const animRadius = new BABYLON.Animation("animR", "radius", fps, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT);
        animRadius.setKeys([{ frame: 0, value: camera.radius }, { frame: totalFrames, value: 60 }]);

        scene.beginDirectAnimation(camera, [animTarget, animRadius], 0, totalFrames, false);
    };

    window.triggerMapResize = function () {
        window.dispatchEvent(new Event('resize'));
    };

    // Render Loop
    engine.runRenderLoop(() => {
        scene.render();
    });

    window.addEventListener("resize", () => {
        engine.resize();
    });
};
