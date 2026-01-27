
window.RaceEngine = {
    scene: null,
    camera: null,
    renderer: null,
    controls: null,
    cars: [],
    clock: new THREE.Clock(),
    state: "IDLE",
    trackRadiusX: 60, // Scaled up
    trackRadiusZ: 30, // Scaled up

    init: async function (containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // 1. Setup Three.js
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x87CEEB);
        this.scene.fog = new THREE.Fog(0x87CEEB, 20, 300); // Increased fog distance

        this.camera = new THREE.PerspectiveCamera(60, container.clientWidth / container.clientHeight, 0.1, 1000);
        this.camera.position.set(0, 40, 90); // Pulled back for bigger view

        this.renderer = new THREE.WebGLRenderer({ antialias: true, logarithmicDepthBuffer: true });
        this.renderer.setSize(container.clientWidth, container.clientHeight);
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        container.innerHTML = '';
        container.appendChild(this.renderer.domElement);

        // 2. Interaction
        if (THREE.OrbitControls) {
            this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
            this.controls.enableDamping = true;
            this.controls.dampingFactor = 0.05;
            this.controls.maxPolarAngle = Math.PI / 2 - 0.05; // Don't go below ground
        }

        // 3. Lighting
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        this.scene.add(ambientLight);

        const dirLight = new THREE.DirectionalLight(0xffffff, 0.9);
        dirLight.position.set(100, 100, 50);
        dirLight.castShadow = true;
        dirLight.shadow.mapSize.width = 2048;
        dirLight.shadow.mapSize.height = 2048;
        dirLight.shadow.camera.left = -100;
        dirLight.shadow.camera.right = 100;
        dirLight.shadow.camera.top = 100;
        dirLight.shadow.camera.bottom = -100;
        this.scene.add(dirLight);

        // 4. Generate Track
        this.createTrackMesh();
        this.createInfieldGrass();

        // 5. Load Assets
        await this.loadScene("Daytona_500_Speedway");

        // 6. Start Loop
        this.animate();

        window.addEventListener('resize', () => {
            this.camera.aspect = container.clientWidth / container.clientHeight;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(container.clientWidth, container.clientHeight);
        });
    },

    createTrackMesh: function () {
        // Create an Oval Shape
        const shape = new THREE.Shape();
        const rx = this.trackRadiusX; // 60
        const rz = this.trackRadiusZ; // 30

        // Outer oval
        shape.absellipse(0, 0, rx + 4, rz + 4, 0, Math.PI * 2, false, 0);

        // Inner oval hole
        const holePath = new THREE.Path();
        holePath.absellipse(0, 0, rx - 4, rz - 4, 0, Math.PI * 2, true, 0);
        shape.holes.push(holePath);

        const geometry = new THREE.ExtrudeGeometry(shape, {
            depth: 0.2,
            bevelEnabled: false,
            curveSegments: 64
        });

        // Rotate flat on ground
        geometry.rotateX(Math.PI / 2);

        const material = new THREE.MeshStandardMaterial({
            color: 0x222222,
            roughness: 0.8
        });

        const track = new THREE.Mesh(geometry, material);
        track.receiveShadow = true;
        this.scene.add(track);

        // Add Yellow Line (Inner) - Todo: Implement with RingGeometry
        // const innerLineGeo = new THREE.BufferGeometry();
    },

    createInfieldGrass: function () {
        const geometry = new THREE.PlaneGeometry(300, 300);
        geometry.rotateX(-Math.PI / 2);
        const material = new THREE.MeshStandardMaterial({ color: 0x114411, roughness: 1 });
        const grass = new THREE.Mesh(geometry, material);
        grass.position.y = -0.1;
        grass.receiveShadow = true;
        this.scene.add(grass);
    },

    loadScene: async function (sceneName) {
        try {
            const response = await fetch(`data/Scenes/${sceneName}.json`);
            const sceneData = await response.json();

            // Load Items
            for (const item of sceneData.Items) {
                // Skip the procedurally generated track section from the JSON if we made our own
                if (item.RecipeName === "Asphalt_Track_Section") continue;

                await this.loadItem(item);
            }
        } catch (err) {
            console.error("Failed to load scene:", err);
        }
    },

    loadItem: async function (item) {
        const typeFolder = item.ArtType === "Procedural" ? "Props" : "Actors";
        const response = await fetch(`data/${typeFolder}/${item.RecipeName}.json`);
        const recipe = await response.json();

        const group = new THREE.Group();

        // Smart Positioning for the Large Track
        // Original Scene (Radius ~10) -> New Track (Radius 60)
        // We identify if things were "Outside" or "Inside" the original track and push them

        // Items roughly at X > 5 are "Outside" (Grandstands, Fence) -> Move to Radius + Margin
        // Items roughly at X < -5 are "Inside" (Pit Wall) -> Move to Radius - Margin

        let posX = item.Position[0];
        let posZ = item.Position[2];

        const originalTrackRadius = 10;
        const newTrackRadiusX = 60;
        const scaleFactor = 1.0;

        if (item.RecipeName.includes("Grandstand") || item.RecipeName.includes("Fence") || item.RecipeName.includes("Searchlight") || item.RecipeName.includes("Policeman")) {
            // These belong on the OUTSIDE of the track
            // Original X was approx 12-18
            const offsetFromEdge = Math.abs(posX) - originalTrackRadius;
            if (posX > 0) posX = newTrackRadiusX + 4 + offsetFromEdge; // Right side
            // (Note: Original scene only had one side populated, so we focus on positive X)
        }
        else if (item.RecipeName.includes("Pit") || item.RecipeName.includes("Mechanic")) {
            // These belong on the INSIDE of the track
            // Original X was approx -10
            const offsetFromEdge = Math.abs(posX) - originalTrackRadius;
            // Pit wall should be just inside
            if (posX < 0) posX = -(newTrackRadiusX - 10); // Move to the *other* side of the oval (inner pit lane)? 
            // Wait, the original scene was linear Z. The new track is an oval.
            // Let's place the pits on the "Front Stretch" (Positive X side, inner)
            // If original was negative X (left of cars), on the oval that's "Inside"
            posX = newTrackRadiusX - 8;
        }
        else if (item.RecipeName.includes("Race_Official")) {
            // Put him on the start line
            posX = newTrackRadiusX + 5;
            posZ = 0;
        }

        group.position.set(posX, item.Position[1], posZ);

        group.rotation.set(
            THREE.MathUtils.degToRad(item.Rotation[0]),
            THREE.MathUtils.degToRad(item.Rotation[1]),
            THREE.MathUtils.degToRad(item.Rotation[2])
        );
        group.scale.set(item.Scale[0], item.Scale[1], item.Scale[2]);

        // Build Parts
        if (recipe.Type === "Procedural" && recipe.Parts) {
            recipe.Parts.forEach(part => {
                const geoName = part.Shape;
                let geometry;
                switch (geoName) {
                    case "Box": geometry = new THREE.BoxGeometry(1, 1, 1); break;
                    case "Cylinder": geometry = new THREE.CylinderGeometry(0.5, 0.5, 1, 16); break;
                    case "Sphere": geometry = new THREE.SphereGeometry(0.5, 16, 16); break;
                    default: geometry = new THREE.BoxGeometry(1, 1, 1); break;
                }

                const material = this.getMaterial(part.ColorHex, part.Material);
                const mesh = new THREE.Mesh(geometry, material);

                mesh.position.set(part.Position[0], part.Position[1], part.Position[2]);
                mesh.rotation.set(
                    THREE.MathUtils.degToRad(part.Rotation[0]),
                    THREE.MathUtils.degToRad(part.Rotation[1]),
                    THREE.MathUtils.degToRad(part.Rotation[2])
                );
                mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);
                mesh.castShadow = true;
                mesh.receiveShadow = true;
                group.add(mesh);
            });
        }
        else if (recipe.Type === "Voxel") {
            const geo = new THREE.BoxGeometry(0.5, 1.8, 0.5);
            const mat = new THREE.MeshStandardMaterial({ color: 0x0000FF });
            const mesh = new THREE.Mesh(geo, mat);
            mesh.position.y = 0.9;
            group.add(mesh);
        }

        this.scene.add(group);

        // Register Cars for Logic
        if (item.RecipeName.includes("Racecar")) {
            this.cars.push({
                mesh: group,
                speed: 0,
                progress: Math.random(),
                lane: (Math.random() - 0.5) * 6, // Winder lanes
                lap: 0
            });
            // Reset car pos to track
            this.updateCarPos(this.cars[this.cars.length - 1]);
        }
    },

    getMaterial: function (colorHex, type) {
        const color = new THREE.Color(colorHex);
        switch (type) {
            case "Metal": return new THREE.MeshStandardMaterial({ color: color, roughness: 0.3, metalness: 0.8 });
            case "Glass": return new THREE.MeshStandardMaterial({ color: color, transparent: true, opacity: 0.6 });
            case "Glow": return new THREE.MeshBasicMaterial({ color: color });
            default: return new THREE.MeshStandardMaterial({ color: color });
        }
    },

    startRaceSequence: function () {
        this.state = "COUNTDOWN_RED";
        setTimeout(() => { this.state = "COUNTDOWN_YELLOW"; }, 1000);
        setTimeout(() => { this.state = "COUNTDOWN_GREEN"; }, 2000);
        setTimeout(() => { this.state = "RACING"; }, 3000);
    },

    stopRace: function () {
        this.state = "IDLE";
        this.cars.forEach(c => c.speed = 0);
    },

    animate: function () {
        requestAnimationFrame(() => this.animate());
        const delta = this.clock.getDelta();

        if (this.controls) this.controls.update();

        if (this.state === "RACING") {
            this.updateCars(delta);
        }

        this.renderer.render(this.scene, this.camera);
    },

    updateCars: function (delta) {
        this.cars.forEach(car => {
            // Accelerate
            if (car.speed < 40) car.speed += 8 * delta; // Faster

            // Move
            car.progress += (car.speed * delta) / 400; // Longer track = larger divider
            if (car.progress > 1) {
                car.progress -= 1;
                car.lap++;
            }
            this.updateCarPos(car);
        });
    },

    updateCarPos: function (car) {
        const rx = this.trackRadiusX;
        const rz = this.trackRadiusZ;

        // Progress 0..1 around the loop
        const angle = car.progress * Math.PI * 2;

        // Ellipse
        const baseX = Math.cos(angle) * rx;
        const baseZ = Math.sin(angle) * rz;

        // Normal vector (outward from center)
        const len = Math.sqrt(baseX * baseX + baseZ * baseZ);
        const normX = baseX / len;
        const normZ = baseZ / len;

        car.mesh.position.x = baseX + (normX * car.lane);
        car.mesh.position.z = baseZ + (normZ * car.lane);

        // Look Ahead
        const nextAngle = (car.progress + 0.005) * Math.PI * 2;
        const nextX = Math.cos(nextAngle) * rx;
        const nextZ = Math.sin(nextAngle) * rz;
        car.mesh.lookAt(nextX + (normX * car.lane), car.mesh.position.y, nextZ + (normZ * car.lane));
    },

    getRaceState: function () {
        return this.state;
    }
};
