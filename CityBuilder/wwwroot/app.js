window.CityBuilder = {
    init: function (containerId) {
        console.log("Initializing CityBuilder...");
        const container = document.getElementById(containerId);
        const statusDiv = document.getElementById('status');
        if (!container) return;

        // --- SCENE SETUP ---
        const scene = new THREE.Scene();
        scene.background = new THREE.Color(0x87CEEB); // Sky Blue
        scene.fog = new THREE.Fog(0x87CEEB, 20, 100);

        const camera = new THREE.PerspectiveCamera(45, container.clientWidth / container.clientHeight, 0.1, 1000);
        camera.position.set(30, 30, 30);
        camera.lookAt(0, 0, 0);

        const renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(container.clientWidth, container.clientHeight);
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        container.appendChild(renderer.domElement);

        // LIGHTING
        const ambi = new THREE.AmbientLight(0xffffff, 0.6);
        scene.add(ambi);

        const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
        dirLight.position.set(10, 50, 20);
        dirLight.castShadow = true;
        dirLight.shadow.mapSize.width = 2048;
        dirLight.shadow.mapSize.height = 2048;
        scene.add(dirLight);

        // GROUND
        const ground = new THREE.Mesh(
            new THREE.PlaneGeometry(100, 100),
            new THREE.MeshStandardMaterial({ color: 0x33aa33 })
        );
        ground.rotation.x = -Math.PI / 2;
        ground.receiveShadow = true;
        scene.add(ground);

        // CONTROLS
        const controls = new THREE.OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;

        // --- DATA STRUCTURES ---

        // A Brick is just {x, y, z, color} (voxel coords)
        // 1 unit = 1 meter

        // --- BLUEPRINTS ---
        function createTowerBlueprint() {
            const bricks = [];
            const height = 15;
            const radius = 5;
            for (let y = 0; y < height; y++) {
                // Circular walls
                const circ = Math.floor(2 * Math.PI * radius);
                for (let i = 0; i < circ; i++) {
                    const angle = (i / circ) * Math.PI * 2;
                    const x = Math.round(Math.cos(angle) * radius);
                    const z = Math.round(Math.sin(angle) * radius);
                    bricks.push({ x, y, z, color: 0x884422 });
                }
            }
            return bricks;
        }

        function createPyramidBlueprint(offsetX, offsetZ) {
            const bricks = [];
            const height = 10;
            for (let y = 0; y < height; y++) {
                const size = height - y;
                // create a square ring at this height
                for (let x = -size; x <= size; x++) {
                    for (let z = -size; z <= size; z++) {
                        // Only edges? Or filled? Let's do filled for a solid pyramid
                        bricks.push({ x: x + offsetX, y: y, z: z + offsetZ, color: 0xddcc55 });
                    }
                }
            }
            return bricks;
        }

        // Combine blueprints
        const blueprint = [
            ...createTowerBlueprint(),           // Tower at 0,0
            ...createPyramidBlueprint(20, -10)   // Pyramid offset by 20, -10
        ];

        // Sort by height so we build bottom-up!
        blueprint.sort((a, b) => a.y - b.y);

        // --- MANAGERS ---
        const PENDING_BRICKS = [...blueprint];
        const ASSIGNED_BRICKS = new Set();
        const PLACED_BRICKS = new THREE.Group();
        scene.add(PLACED_BRICKS);

        // Supply Pile (Source of bricks)
        const SUPPLY_POS = new THREE.Vector3(-15, 0, -15);
        const pileMesh = new THREE.Mesh(
            new THREE.BoxGeometry(4, 2, 4),
            new THREE.MeshStandardMaterial({ color: 0xaa5533 })
        );
        pileMesh.position.copy(SUPPLY_POS);
        pileMesh.position.y = 1;
        pileMesh.castShadow = true;
        scene.add(pileMesh);

        // --- ROBOT AGENT ---
        class Robot {
            constructor(id) {
                this.id = id;
                this.state = 'IDLE'; // IDLE, FETCH, CARRY, BUILD
                this.targetBrick = null; // {x,y,z,color}
                this.position = new THREE.Vector3(Math.random() * 10, 0, Math.random() * 10);
                this.speed = 8.0;
                this.carryObj = null;

                // Visuals
                this.mesh = new THREE.Group();
                const body = new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1), new THREE.MeshStandardMaterial({ color: 0xcccccc }));
                body.position.y = 0.5;
                body.castShadow = true;
                this.mesh.add(body);

                const head = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.4, 0.6), new THREE.MeshStandardMaterial({ color: 0x4488ff }));
                head.position.y = 1.2;
                this.mesh.add(head);

                scene.add(this.mesh);
                this.mesh.position.copy(this.position);
            }

            update(dt) {
                switch (this.state) {
                    case 'IDLE':
                        // Ask manager for work
                        if (PENDING_BRICKS.length > 0) {
                            // Find next available (bottom-up is already sorted)
                            this.targetBrick = PENDING_BRICKS.shift();
                            // Ensure we don't pick one that is unsupported?
                            // For simplicity, we trust the sort order (layer by layer)
                            this.state = 'FETCH';
                        }
                        break;

                    case 'FETCH':
                        if (this.moveTo(SUPPLY_POS, dt)) {
                            // Arrived at supply
                            this.state = 'CARRY';
                            // "Pick up" brick
                            this.carryObj = new THREE.Mesh(new THREE.BoxGeometry(0.8, 0.8, 0.8), new THREE.MeshStandardMaterial({ color: this.targetBrick.color }));
                            this.carryObj.position.set(0, 1.5, 0);
                            this.mesh.add(this.carryObj);
                        }
                        break;

                    case 'CARRY':
                        const buildPos = new THREE.Vector3(this.targetBrick.x, this.targetBrick.y, this.targetBrick.z);
                        // Move to "near" the build pos, but on the ground? 
                        // Or fly? Let's say they are ground robots that can "reach" up or climb?
                        // Let's make them hover drones for simplicity of 3D construction?
                        // Or ground robots that stand at X, Z and "build" Y.
                        const targetStand = new THREE.Vector3(this.targetBrick.x, 0, this.targetBrick.z);
                        // Offset slightly so they don't stand IN the wall
                        targetStand.add(new THREE.Vector3(2, 0, 2).normalize().multiplyScalar(2));

                        if (this.moveTo(targetStand, dt)) {
                            this.state = 'BUILD';
                            this.buildTimer = 0.5; // Seconds to build
                        }
                        break;

                    case 'BUILD':
                        this.buildTimer -= dt;
                        if (this.buildTimer <= 0) {
                            // Place it
                            if (this.carryObj) {
                                this.mesh.remove(this.carryObj);
                                this.carryObj = null;
                            }

                            const brick = new THREE.Mesh(
                                new THREE.BoxGeometry(1, 1, 1),
                                new THREE.MeshStandardMaterial({ color: this.targetBrick.color })
                            );
                            brick.position.set(this.targetBrick.x, this.targetBrick.y + 0.5, this.targetBrick.z);
                            brick.castShadow = true;
                            brick.receiveShadow = true;
                            PLACED_BRICKS.add(brick);

                            // Dust particle visual?

                            this.targetBrick = null;
                            this.state = 'IDLE';
                        }
                        break;
                }
            }

            moveTo(target, dt) {
                const dir = new THREE.Vector3().subVectors(target, this.mesh.position);
                dir.y = 0; // Keep on ground (unless we want flying)
                const dist = dir.length();
                if (dist < 0.5) return true;

                dir.normalize();
                this.mesh.position.addScaledVector(dir, this.speed * dt);
                this.mesh.lookAt(target.x, this.mesh.position.y, target.z);
                return false;
            }
        }

        const robots = [];
        for (let i = 0; i < 5; i++) robots.push(new Robot(i));

        // --- LOOP ---
        const clock = new THREE.Clock();

        function animate() {
            requestAnimationFrame(animate);
            const dt = clock.getDelta();
            controls.update();

            robots.forEach(r => r.update(dt));

            // Status Update
            const pct = Math.floor(100 * (blueprint.length - PENDING_BRICKS.length) / blueprint.length);
            statusDiv.innerText = `Pending: ${PENDING_BRICKS.length} | Progress: ${pct}%`;

            renderer.render(scene, camera);
        }
        animate();
    }
};
