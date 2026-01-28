window.universalMap = {
    engine: null,
    scene: null,
    camera: null,
    advancedTexture: null,
    dotNetHelper: null, // Holds reference to Blazor

    setupClick: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
    },

    init: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        this.engine = new BABYLON.Engine(canvas, true);

        // Create Scene
        this.scene = new BABYLON.Scene(this.engine);
        this.scene.clearColor = new BABYLON.Color4(0, 0, 0, 1); // Pure Black Background

        // Camera
        this.camera = new BABYLON.ArcRotateCamera("camera", -Math.PI / 2, Math.PI / 3, 50, new BABYLON.Vector3(0, 0, 0), this.scene);
        this.camera.attachControl(canvas, true);
        this.camera.lowerRadiusLimit = 10;
        this.camera.upperRadiusLimit = 200;

        // Light
        const light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(0, 1, 0), this.scene);
        light.intensity = 1.0;

        // GUI
        this.advancedTexture = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("UI");

        // Run Loop
        this.engine.runRenderLoop(() => {
            this.scene.render();
        });

        // Resize
        window.addEventListener("resize", () => {
            this.engine.resize();
        });

        this.loadGeoJSON();
    },

    loadGeoJSON: async function () {
        // Fetch US States
        const response = await fetch('data/us-states.json');
        const data = await response.json();

        // Material for States - Vibrant Blue
        const stateMat = new BABYLON.StandardMaterial("stateMat", this.scene);
        stateMat.diffuseColor = new BABYLON.Color3(0.0, 0.4, 0.8); // Dodger Blue
        stateMat.emissiveColor = new BABYLON.Color3(0.0, 0.1, 0.2);
        stateMat.alpha = 0.9;
        stateMat.backFaceCulling = false;

        // Render States
        data.features.forEach(feature => {
            if (feature.geometry.type === "Polygon") {
                this.createPolygon(feature.geometry.coordinates, stateMat);
            } else if (feature.geometry.type === "MultiPolygon") {
                feature.geometry.coordinates.forEach(polygon => {
                    this.createPolygon(polygon, stateMat);
                });
            }
        });
    },

    createPolygon: function (coordinates, material) {
        const shape = [];
        // Handle nested arrays in GeoJSON
        const ring = coordinates[0];

        ring.forEach(coord => {
            // GeoJSON is [Lon, Lat]. Map to X, Z.
            const x = (coord[0] + 96) * 2;
            const z = (coord[1] - 37) * 2;
            shape.push(new BABYLON.Vector3(x, 0, z));
        });

        const polygon = BABYLON.MeshBuilder.CreatePolygon("polygon", { shape: shape, sideOrientation: BABYLON.Mesh.DOUBLESIDE }, this.scene);
        polygon.material = material;

        // Add Blue Border Lines for that "Tron" look
        const points = shape.map(p => new BABYLON.Vector3(p.x, 0.05, p.z));
        points.push(points[0]); // Close loop
        const lines = BABYLON.MeshBuilder.CreateLines("lines", { points: points }, this.scene);
        lines.color = new BABYLON.Color3(0.2, 0.6, 1.0); // Light Blue
    },

    pylons: [],
    labels: [],

    renderPylons: function (locations) {
        // Clear existing items
        this.pylons.forEach(p => p.dispose());
        this.pylons = [];

        this.labels.forEach(l => l.dispose());
        this.labels = [];

        const pylonMat = new BABYLON.StandardMaterial("pylonMat", this.scene);
        pylonMat.emissiveColor = new BABYLON.Color3(0, 1, 1);
        pylonMat.diffuseColor = new BABYLON.Color3(0, 1, 1);

        locations.forEach(loc => {
            if (loc.latitude && loc.longitude) {
                const x = (loc.longitude + 96) * 2;
                const z = (loc.latitude - 37) * 2;

                // Create Material Per Pylon (Simplification)
                const mat = new BABYLON.StandardMaterial("pylonMat_" + loc.name, this.scene);
                mat.emissiveColor = BABYLON.Color3.FromHexString(loc.color || "#00ffff");
                mat.diffuseColor = BABYLON.Color3.FromHexString(loc.color || "#00ffff");

                // Create Cone (Teardrop shape)
                const pylon = BABYLON.MeshBuilder.CreateCylinder("pylon", { height: 3, diameterTop: 0, diameterBottom: 1, tessellation: 8 }, this.scene);
                pylon.position = new BABYLON.Vector3(x, 1.5, z);
                pylon.material = mat;

                this.pylons.push(pylon); // Track for cleanup

                // Click Action
                pylon.actionManager = new BABYLON.ActionManager(this.scene);
                pylon.actionManager.registerAction(new BABYLON.ExecuteCodeAction(BABYLON.ActionManager.OnPickTrigger, () => {
                    if (this.dotNetHelper) {
                        this.dotNetHelper.invokeMethodAsync("OnLocationClicked", loc.name);
                    }
                }));

                // Add Label
                this.createLabel(loc.name, pylon);
            }
        });
    },

    createLabel: function (text, mesh) {
        const label = new BABYLON.GUI.Rectangle();
        label.width = "100px";
        label.height = "30px";
        label.color = "white";
        label.thickness = 0;
        label.background = "transparent";
        this.advancedTexture.addControl(label);
        label.linkWithMesh(mesh);
        label.linkOffsetY = -40;

        this.labels.push(label); // Track for cleanup

        const textBlock = new BABYLON.GUI.TextBlock();
        textBlock.text = text;
        textBlock.color = "white";
        textBlock.fontWeight = "bold";
        textBlock.fontSize = 14;
        textBlock.outlineWidth = 2;
        textBlock.outlineColor = "black";
        label.addControl(textBlock);

        // Click on Label too
        label.isPointerBlocker = true;
        label.onPointerUpObservable.add(() => {
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync("OnLocationClicked", text);
            }
        });
    }
};
