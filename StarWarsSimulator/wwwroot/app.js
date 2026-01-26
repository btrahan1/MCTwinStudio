window.StarWarsSimulator = {
    init: async function (containerId) {
        console.log("Initializing StarWarsSimulator...");

        const container = document.getElementById(containerId);
        if (!container) {
            console.error("Container not found:", containerId);
            return;
        }

        // Clear existing children
        while (container.firstChild) {
            container.removeChild(container.firstChild);
        }

        // Create UI Overlay for Game Over
        const statusText = document.createElement('div');
        statusText.style.position = 'absolute';
        statusText.style.top = '50%';
        statusText.style.left = '50%';
        statusText.style.transform = 'translate(-50%, -50%)';
        statusText.style.color = 'yellow';
        statusText.style.fontFamily = 'Arial, sans-serif';
        statusText.style.fontSize = '48px';
        statusText.style.fontWeight = 'bold';
        statusText.style.textShadow = '0 0 10px red';
        statusText.style.display = 'none';
        statusText.style.pointerEvents = 'none';
        container.appendChild(statusText);

        const scene = new THREE.Scene();
        const camera = new THREE.PerspectiveCamera(75, container.clientWidth / container.clientHeight, 0.1, 1000);
        const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });

        renderer.setSize(container.clientWidth, container.clientHeight);
        container.appendChild(renderer.domElement);

        // Resize handler
        const onResize = () => {
            if (!container) return;
            const width = container.clientWidth;
            const height = container.clientHeight;
            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height);
        };
        window.addEventListener('resize', onResize);

        // Lighting
        const ambientLight = new THREE.AmbientLight(0x404040, 2);
        scene.add(ambientLight);
        const directionalLight = new THREE.DirectionalLight(0xffffff, 1);
        directionalLight.position.set(10, 10, 10);
        scene.add(directionalLight);
        const pointLight = new THREE.PointLight(0xffffff, 1, 100);
        pointLight.position.set(0, 0, 0);
        scene.add(pointLight);

        // Camera positioning
        camera.position.z = 60;
        camera.position.y = 20;
        camera.lookAt(0, 0, 0);

        // OrbitControls
        const controls = new THREE.OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        controls.dampingFactor = 0.05;

        // Group to hold everything
        const worldGroup = new THREE.Group();
        scene.add(worldGroup);

        // Load Props
        const deathStar = await loadProp('props/deathstar.json');
        if (deathStar) {
            deathStar.position.set(0, 0, 0);
            deathStar.scale.set(3, 3, 3);
            worldGroup.add(deathStar);
        }

        const xwings = [];
        const tieFighters = [];

        // Spawn Fighters Helper
        async function spawnFighter(type, count) {
            const list = type === 'xwing' ? xwings : tieFighters;
            const url = type === 'xwing' ? 'props/xwing.json' : 'props/tiefighter.json';

            for (let i = 0; i < count; i++) {
                const fighter = await loadProp(url);
                if (fighter) {
                    const radius = 35 + Math.random() * 10;
                    const angle = Math.random() * Math.PI * 2;

                    fighter.userData = {
                        type: type,
                        speed: 0.005 + Math.random() * 0.005, // Super slow speed
                        angle: angle,
                        radius: radius,
                        state: 'ORBITING',
                        stateTimer: Math.random() * 500,
                        currentRadius: radius,
                        yOffset: (Math.random() - 0.5) * 10
                    };

                    fighter.position.set(
                        Math.cos(angle) * radius,
                        fighter.userData.yOffset,
                        Math.sin(angle) * radius
                    );

                    worldGroup.add(fighter);
                    list.push(fighter);
                }
            }
        }

        await spawnFighter('xwing', 10); // More fighters for better battle
        await spawnFighter('tie', 10);

        // Lasers & Explosions
        const lasers = [];
        const explosions = [];
        const laserMaterialRed = new THREE.MeshBasicMaterial({ color: 0xff0000 });
        const laserMaterialGreen = new THREE.MeshBasicMaterial({ color: 0x00ff00 });
        const laserGeometry = new THREE.CylinderGeometry(0.1, 0.1, 4, 8);
        laserGeometry.rotateX(Math.PI / 2);

        function shootLaser(source, target) {
            const laser = new THREE.Mesh(laserGeometry, source.userData.type === 'xwing' ? laserMaterialRed : laserMaterialGreen);
            laser.position.copy(source.position);
            laser.lookAt(target.position);
            const direction = new THREE.Vector3().subVectors(target.position, source.position).normalize();
            laser.userData = {
                velocity: direction.multiplyScalar(1.0), // Slower lasers
                life: 200, // Longer life since they are slower
                ownerType: source.userData.type
            };
            worldGroup.add(laser);
            lasers.push(laser);
        }

        function createExplosion(position) {
            const particleCount = 20;
            const geometry = new THREE.BoxGeometry(0.5, 0.5, 0.5);
            const material = new THREE.MeshBasicMaterial({ color: 0xffaa00 });

            for (let i = 0; i < particleCount; i++) {
                const particle = new THREE.Mesh(geometry, material);
                particle.position.copy(position);

                // Random velocity
                particle.userData = {
                    velocity: new THREE.Vector3(
                        (Math.random() - 0.5) * 1.5,
                        (Math.random() - 0.5) * 1.5,
                        (Math.random() - 0.5) * 1.5
                    ),
                    life: 30 + Math.random() * 20
                };

                worldGroup.add(particle);
                explosions.push(particle);
            }
        }

        let gameOver = false;

        // Animation Loop
        function animate() {
            if (gameOver) return;

            requestAnimationFrame(animate);
            controls.update();

            // Rotate Death Star
            if (deathStar) {
                deathStar.rotation.y += 0.001; // Slower rotation
            }

            // Move Fighters
            [...xwings, ...tieFighters].forEach(fighter => {
                const data = fighter.userData;
                data.angle += data.speed;
                data.stateTimer--;

                // State Machine (Simplified/Maintained)
                if (data.state === 'ORBITING') {
                    if (data.stateTimer <= 0) {
                        data.state = 'DISENGAGING';
                        data.stateTimer = 200 + Math.random() * 200;
                    }
                } else if (data.state === 'DISENGAGING') {
                    data.currentRadius += 0.075; // Even slower outward
                    if (data.currentRadius > 150 || data.stateTimer <= 0) {
                        data.state = 'RETURNING';
                    }
                } else if (data.state === 'RETURNING') {
                    data.currentRadius -= 0.15; // Slower return
                    if (data.currentRadius <= data.radius) {
                        data.currentRadius = data.radius;
                        data.state = 'ORBITING';
                        data.stateTimer = 300 + Math.random() * 500;
                    }
                }

                fighter.position.x = Math.cos(data.angle) * data.currentRadius;
                fighter.position.z = Math.sin(data.angle) * data.currentRadius;
                fighter.position.y = data.yOffset + Math.sin(data.angle * 3) * 2;

                const targetX = Math.cos(data.angle + 0.1) * data.currentRadius;
                const targetZ = Math.sin(data.angle + 0.1) * data.currentRadius;
                fighter.lookAt(targetX, fighter.position.y, targetZ);

                // Shooting
                if (Math.random() < 0.03) {
                    const targets = fighter.userData.type === 'xwing' ? tieFighters : xwings;
                    if (targets.length > 0) {
                        const target = targets[Math.floor(Math.random() * targets.length)];
                        shootLaser(fighter, target);
                    }
                }
            });

            // Update Lasers & Collisions
            for (let i = lasers.length - 1; i >= 0; i--) {
                const laser = lasers[i];
                laser.position.add(laser.userData.velocity);
                laser.userData.life--;

                let hit = false;
                const targets = laser.userData.ownerType === 'xwing' ? tieFighters : xwings;

                for (let j = targets.length - 1; j >= 0; j--) {
                    const target = targets[j];
                    if (laser.position.distanceTo(target.position) < 3.0) { // Collision Threshold
                        hit = true;
                        createExplosion(target.position);

                        // Remove Target
                        worldGroup.remove(target);
                        targets.splice(j, 1);

                        // Check Win Condition
                        if (targets.length === 0) {
                            const winner = laser.userData.ownerType === 'xwing' ? "REBELS WIN!" : "EMPIRE WINS!";
                            statusText.innerText = winner;
                            statusText.style.display = 'block';
                            // Don't stop animation immediately, let explosions finish? No, let's keep running but maybe stop shooting?
                            // User asked "simulation ends". Let's handle it gracefully.
                        }
                        break;
                    }
                }

                if (hit || laser.userData.life <= 0) {
                    worldGroup.remove(laser);
                    lasers.splice(i, 1);
                }
            }

            // Update Explosions
            for (let i = explosions.length - 1; i >= 0; i--) {
                const particle = explosions[i];
                particle.position.add(particle.userData.velocity);
                particle.userData.life--;
                particle.material.opacity = particle.userData.life / 50;
                particle.scale.multiplyScalar(0.95); // shrink

                if (particle.userData.life <= 0) {
                    worldGroup.remove(particle);
                    explosions.splice(i, 1);
                }
            }

            renderer.render(scene, camera);
        }

        animate();
    }
};

async function loadProp(url) {
    // ... (Existing loadProp code remains exactly the same, omitting for brevity in tool call if possible, but Overwrite needs full file)
    // I will include the full loadProp to ensure integrity.
    try {
        const response = await fetch(url);
        const data = await response.json();
        const group = new THREE.Group();

        if (data.Parts) {
            data.Parts.forEach(part => {
                let geometry;
                if (part.Shape === 'Sphere') {
                    geometry = new THREE.SphereGeometry(1, 16, 16);
                } else if (part.Shape === 'Torus') {
                    geometry = new THREE.TorusGeometry(0.5, 0.05, 8, 24);
                } else if (part.Shape === 'Cylinder') {
                    geometry = new THREE.CylinderGeometry(0.5, 0.5, 1, 16);
                } else {
                    geometry = new THREE.BoxGeometry(1, 1, 1);
                }

                const material = new THREE.MeshStandardMaterial({
                    color: part.ColorHex,
                    roughness: 0.5,
                    metalness: part.Material === 'Metal' ? 0.8 : 0.0,
                    emissive: part.Material === 'Glow' ? part.ColorHex : 0x000000
                });

                const mesh = new THREE.Mesh(geometry, material);

                if (part.Position) mesh.position.set(part.Position[0], part.Position[1], part.Position[2]);
                if (part.Rotation) mesh.rotation.set(
                    part.Rotation[0] * Math.PI / 180,
                    part.Rotation[1] * Math.PI / 180,
                    part.Rotation[2] * Math.PI / 180
                );

                if (part.Scale) {
                    if (part.Shape === 'Torus') {
                        mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);
                    } else if (part.Shape === 'Sphere') {
                        mesh.geometry = new THREE.SphereGeometry(0.5, 32, 32);
                        mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);
                    }
                    else if (part.Shape === 'Cylinder') {
                        mesh.geometry = new THREE.CylinderGeometry(0.5, 0.5, 1, 32);
                        mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);
                    }
                    else {
                        mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);
                    }
                }
                group.add(mesh);
            });
        }
        return group;
    } catch (e) {
        console.error("Error loading prop:", url, e);
        return null;
    }
}
