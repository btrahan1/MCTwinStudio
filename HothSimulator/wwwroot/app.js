window.HothSimulator = {
    init: async function (containerId) {
        console.log("Initializing HothSimulator...");

        const container = document.getElementById(containerId);
        if (!container) return;

        // Clear existing
        while (container.firstChild) container.removeChild(container.firstChild);

        // Scene Setup
        const scene = new THREE.Scene();
        scene.background = new THREE.Color(0xaaccff); // Sky blue
        scene.fog = new THREE.Fog(0xaaccff, 20, 300); // White/Blue fog

        const camera = new THREE.PerspectiveCamera(75, container.clientWidth / container.clientHeight, 0.1, 1000);
        const renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(container.clientWidth, container.clientHeight);
        renderer.shadowMap.enabled = true;
        container.appendChild(renderer.domElement);

        // Lighting
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6); // Bright snowy ambient
        scene.add(ambientLight);

        const dirLight = new THREE.DirectionalLight(0xffffff, 1.2);
        dirLight.position.set(50, 100, 50);
        dirLight.castShadow = true;
        scene.add(dirLight);

        // Ground (Snow)
        const planeGeometry = new THREE.PlaneGeometry(500, 500);
        const planeMaterial = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 1.0 });
        const plane = new THREE.Mesh(planeGeometry, planeMaterial);
        plane.rotation.x = -Math.PI / 2;
        plane.receiveShadow = true;
        scene.add(plane);

        // Camera Controls
        const controls = new THREE.OrbitControls(camera, renderer.domElement);
        camera.position.set(0, 30, 80);
        controls.update();

        // Assets Lists
        const walkers = [];
        const speeders = [];
        const rebels = [];
        const stormtroopers = [];
        const lasers = [];

        // --- ASSET LOADING ---
        async function loadProp(url, scale = 1.0) {
            try {
                const response = await fetch(url);
                const data = await response.json();
                const group = new THREE.Group();

                if (data.Parts) {
                    data.Parts.forEach(part => {
                        let geometry;
                        if (part.Shape === 'Sphere') geometry = new THREE.SphereGeometry(1, 16, 16);
                        else if (part.Shape === 'Torus') geometry = new THREE.TorusGeometry(0.5, 0.05, 8, 24);
                        else if (part.Shape === 'Cylinder') geometry = new THREE.CylinderGeometry(0.5, 0.5, 1, 16);
                        else if (part.Shape === 'Cone') geometry = new THREE.ConeGeometry(0.5, 1, 16);
                        else geometry = new THREE.BoxGeometry(1, 1, 1);

                        const material = new THREE.MeshStandardMaterial({
                            color: part.ColorHex,
                            roughness: 0.5,
                            metalness: part.Material === 'Metal' ? 0.8 : 0.0,
                            emissive: part.Material === 'Glow' ? part.ColorHex : 0x000000
                        });

                        const mesh = new THREE.Mesh(geometry, material);
                        if (part.Id) mesh.name = part.Id; // Identify part for animation
                        if (part.Position) mesh.position.set(part.Position[0], part.Position[1], part.Position[2]);
                        if (part.Rotation) mesh.rotation.set(part.Rotation[0] * Math.PI / 180, part.Rotation[1] * Math.PI / 180, part.Rotation[2] * Math.PI / 180);

                        if (part.Scale) mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);

                        mesh.castShadow = true;
                        mesh.receiveShadow = true;
                        group.add(mesh);
                    });
                }
                group.scale.set(scale, scale, scale);
                return group;
            } catch (e) {
                console.error("Error loading:", url, e);
                return null;
            }
        }

        // --- SPAWN ASSETS ---

        // Shield Generator (Target)
        const shieldGen = await loadProp('props/shield_generator.json', 3);
        if (shieldGen) {
            shieldGen.position.set(0, 0, -100);
            scene.add(shieldGen);
        }

        // AT-AT Walkers (Empire)
        for (let i = -1; i <= 1; i++) {
            const walker = await loadProp('props/at_at.json', 2); // Scales big
            if (walker) {
                walker.position.set(i * 30, 0, 50); // Start far

                // Cache leg parts
                const legs = {
                    fl: walker.getObjectByName('leg_fl_upper'),
                    fr: walker.getObjectByName('leg_fr_upper'),
                    bl: walker.getObjectByName('leg_bl_upper'),
                    br: walker.getObjectByName('leg_br_upper')
                };

                walker.userData = { speed: 0.05, health: 50, legs: legs };
                scene.add(walker);
                walkers.push(walker);
            }
        }

        // Snowspeeders (Rebels)
        for (let i = 0; i < 4; i++) {
            const speeder = await loadProp('props/snowspeeder.json', 0.5);
            if (speeder) {
                speeder.position.set((Math.random() - 0.5) * 100, 10 + Math.random() * 10, 0);
                speeder.userData = {
                    speed: 0.3 + Math.random() * 0.2,
                    angle: Math.random() * Math.PI * 2,
                    radius: 30 + Math.random() * 20,
                    targetWalker: null
                };
                scene.add(speeder);
                speeders.push(speeder);
            }
        }

        // Troops Setup
        async function spawnTroops(typeProp, count, xRange, zRange, list) {
            for (let i = 0; i < count; i++) {
                const troop = await loadProp(typeProp, 0.5); // Human scale approx
                if (troop) {
                    troop.position.set(
                        (Math.random() - 0.5) * xRange,
                        0,
                        zRange + (Math.random() - 0.5) * 20
                    );
                    scene.add(troop);
                    list.push(troop);
                }
            }
        }

        // Rebel Troops (Near Shield Gen)
        await spawnTroops('props/rebel.json', 20, 100, -80, rebels);
        await spawnTroops('props/wookiee.json', 5, 100, -80, rebels);

        // Stormtroopers (With Walkers)
        await spawnTroops('props/stormtrooper.json', 30, 100, 40, stormtroopers);


        // --- LOGIC ---
        const laserGeo = new THREE.CylinderGeometry(0.05, 0.05, 2, 8);
        laserGeo.rotateX(Math.PI / 2);
        const matRed = new THREE.MeshBasicMaterial({ color: 0xff0000 }); // Rebels
        const matGreen = new THREE.MeshBasicMaterial({ color: 0x00ff00 }); // Empire

        function shoot(source, target, colorMat) {
            const laser = new THREE.Mesh(laserGeo, colorMat);
            laser.position.copy(source.position);
            laser.position.y += 1.5; // Shoot from "chest" / "guns"
            laser.lookAt(target.position);

            const dir = new THREE.Vector3().subVectors(target.position, source.position).normalize();
            laser.userData = { velocity: dir.multiplyScalar(2.0), life: 60 };

            scene.add(laser);
            lasers.push(laser);
        }

        function animate() {
            requestAnimationFrame(animate);
            controls.update();

            // Walkers March
            const time = Date.now() * 0.001;
            walkers.forEach(walker => {
                walker.position.z -= walker.userData.speed;

                // Leg Animation (Sinusoidal gait)
                const stride = Math.sin(time * 1.5) * 0.3;
                if (walker.userData.legs) {
                    if (walker.userData.legs.fl) walker.userData.legs.fl.rotation.x = 0.17 + stride;
                    if (walker.userData.legs.br) walker.userData.legs.br.rotation.x = 0.08 + stride;
                    if (walker.userData.legs.fr) walker.userData.legs.fr.rotation.x = -0.08 - stride;
                    if (walker.userData.legs.bl) walker.userData.legs.bl.rotation.x = -0.17 - stride;
                }

                // Body sway and bob
                walker.position.y = Math.abs(Math.sin(time * 3)) * 0.2;
                if (walker.children[0]) walker.rotation.z = Math.sin(time) * 0.02; // Sway whole group slightly

                // Shoot at Shield Gen mainly
                if (Math.random() < 0.01 && shieldGen) {
                    shoot(walker, shieldGen, matGreen);
                }
            });

            // Speeders Fly
            speeders.forEach((speeder, idx) => {
                const data = speeder.userData;
                data.angle += 0.01;

                // Pick a target walker
                if (!data.targetWalker && walkers.length > 0) {
                    data.targetWalker = walkers[idx % walkers.length];
                }

                if (data.targetWalker) {
                    // Orbit walker
                    speeder.position.x = data.targetWalker.position.x + Math.cos(data.angle) * data.radius;
                    speeder.position.z = data.targetWalker.position.z + Math.sin(data.angle) * data.radius;
                    speeder.lookAt(data.targetWalker.position); // Look at target

                    // Shoot
                    if (Math.random() < 0.05) {
                        shoot(speeder, data.targetWalker, matRed);
                    }
                } else {
                    // Roam
                    speeder.position.z += 0.1;
                }
            });

            // Ground Battle
            // Stormtroopers advance
            stormtroopers.forEach(trooper => {
                trooper.position.z -= 0.02;
                // Random shots at rebels
                if (Math.random() < 0.005 && rebels.length > 0) {
                    const target = rebels[Math.floor(Math.random() * rebels.length)];
                    shoot(trooper, target, matGreen);
                }
            });

            rebels.forEach(rebel => {
                if (Math.random() < 0.005 && stormtroopers.length > 0) {
                    const target = stormtroopers[Math.floor(Math.random() * stormtroopers.length)];
                    shoot(rebel, target, matRed);
                }
            });

            // Lasers
            for (let i = lasers.length - 1; i >= 0; i--) {
                const l = lasers[i];
                l.position.add(l.userData.velocity);
                l.userData.life--;
                if (l.userData.life <= 0) {
                    scene.remove(l);
                    lasers.splice(i, 1);
                }
            }

            renderer.render(scene, camera);
        }

        animate();
    }
};
