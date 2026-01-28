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
        if (this.engine) {
            this.engine.dispose();
            this.engine = null;
        }

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
    },
    flyToLocation: function (lat, lon) {
        if (!this.camera || !this.scene) return;

        const x = (lon + 96) * 2;
        const z = (lat - 37) * 2;

        const targetPosition = new BABYLON.Vector3(x, 0, z);

        // Animate Camera Target (Faster: 30 frames at 60fps = 0.5s)
        BABYLON.Animation.CreateAndStartAnimation("camTarget", this.camera, "target", 60, 30, this.camera.target, targetPosition, 2);

        // Animate Zoom (Radius) - Stop closer (30)
        BABYLON.Animation.CreateAndStartAnimation("camRadius", this.camera, "radius", 60, 30, this.camera.radius, 30, 2);

        // Animate Viewing Angle (Beta) - Lock to 45 degrees (approx 0.8 rad) for consistency
        BABYLON.Animation.CreateAndStartAnimation("camBeta", this.camera, "beta", 60, 30, this.camera.beta, 0.8, 2);
    },

    resetCamera: function () {
        if (!this.camera || !this.scene) return;

        // Reset to initial values (Radius 50, Target 0,0,0, Alpha/Beta default)

        BABYLON.Animation.CreateAndStartAnimation("resetTarget", this.camera, "target", 60, 30, this.camera.target, new BABYLON.Vector3(0, 0, 0), 2);
        BABYLON.Animation.CreateAndStartAnimation("resetRadius", this.camera, "radius", 60, 30, this.camera.radius, 50, 2);
        BABYLON.Animation.CreateAndStartAnimation("resetBeta", this.camera, "beta", 60, 30, this.camera.beta, Math.PI / 3, 2);
    },

    makeDraggable: function (elementId) {
        const elm = document.getElementById(elementId);
        if (!elm) return;

        // Header is the handle
        const header = elm.querySelector('.card-header');
        if (!header) return;

        header.style.cursor = 'move';

        let pos1 = 0, pos2 = 0, pos3 = 0, pos4 = 0;

        header.onmousedown = dragMouseDown;

        function dragMouseDown(e) {
            e = e || window.event;
            e.preventDefault();
            // get the mouse cursor position at startup:
            pos3 = e.clientX;
            pos4 = e.clientY;
            document.onmouseup = closeDragElement;
            // call a function whenever the cursor moves:
            document.onmousemove = elementDrag;
        }

        function elementDrag(e) {
            e = e || window.event;
            e.preventDefault();
            // calculate the new cursor position:
            pos1 = pos3 - e.clientX;
            pos2 = pos4 - e.clientY;
            pos3 = e.clientX;
            pos4 = e.clientY;
            // set the element's new position:
            elm.style.top = (elm.offsetTop - pos2) + "px";
            elm.style.left = (elm.offsetLeft - pos1) + "px";
            // Remove 'right' or 'transform' if they exist to allow free movement
            elm.style.right = 'auto';
            elm.style.transform = 'none';
        }

        function closeDragElement() {
            // stop moving when mouse button is released:
            document.onmouseup = null;
            document.onmousemove = null;
        }
    }
};
