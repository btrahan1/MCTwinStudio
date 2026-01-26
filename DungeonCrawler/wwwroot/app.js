window.DungeonCrawler = {
    init: async function (containerId) {
        console.log("Initializing DungeonCrawler (Final Balance)...");

        const container = document.getElementById(containerId);
        if (!container) return;
        while (container.firstChild) container.removeChild(container.firstChild);

        // Vars
        let scene, camera, renderer, controls;
        let entities = [];
        let torches = [];

        // Scene
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x050505);
        scene.fog = new THREE.Fog(0x050505, 10, 60);

        camera = new THREE.PerspectiveCamera(60, container.clientWidth / container.clientHeight, 0.1, 1000);
        camera.position.set(0, 20, 15);
        camera.lookAt(0, 0, 0);

        renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(container.clientWidth, container.clientHeight);
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        container.appendChild(renderer.domElement);

        // --- GLOBAL ILLUMINATION (Soft, Even Light) ---
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        scene.add(ambientLight);

        const hemiLight = new THREE.HemisphereLight(0xffffff, 0x444444, 1.2);
        scene.add(hemiLight);

        // --- SUN/MOON (Shadows) ---
        const dirLight = new THREE.DirectionalLight(0xffffff, 1.0);
        dirLight.position.set(20, 50, 20);
        dirLight.castShadow = true;
        dirLight.shadow.mapSize.width = 4096;
        dirLight.shadow.mapSize.height = 4096;
        dirLight.shadow.camera.left = -50;
        dirLight.shadow.camera.right = 50;
        dirLight.shadow.camera.top = 50;
        dirLight.shadow.camera.bottom = -50;
        scene.add(dirLight);

        // Controls
        if (typeof THREE.OrbitControls !== 'undefined') {
            controls = new THREE.OrbitControls(camera, renderer.domElement);
            controls.target.set(0, 0, 0);
            controls.update();
        }

        // Texture Helper
        function makeTex(hexArray) {
            if (!hexArray || hexArray.length < 64) return null;
            const size = 8;
            const d = new Uint8Array(size * size * 3);
            for (let i = 0; i < size * size; i++) {
                const hex = parseInt(hexArray[i].replace('#', ''), 16);
                d[i * 3] = (hex >> 16) & 255;
                d[i * 3 + 1] = (hex >> 8) & 255;
                d[i * 3 + 2] = hex & 255;
            }
            const tex = new THREE.DataTexture(d, size, size, THREE.RGBFormat);
            tex.magFilter = THREE.NearestFilter;
            tex.needsUpdate = true;
            return tex;
        }

        // Load Prop Helper (Voxel V2 + Weapon)
        async function loadProp(url, scale = 1.0) {
            try {
                // console.log("Loading:", url);
                const response = await fetch(url);
                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                const data = await response.json();
                const group = new THREE.Group();

                // Voxel / Humanoid Loader
                if (data.Type === 'Voxel') {
                    const colors = data.ProceduralColors || { Skin: 0x885533, Shirt: 0x335588, Pants: 0x222222 };

                    const texFace = data.Textures ? makeTex(data.Textures.Face) : null;
                    const texChest = data.Textures ? makeTex(data.Textures.Chest) : null;
                    const texLegs = data.Textures ? makeTex(data.Textures.Legs) : null;

                    // Materials
                    const matSkinBase = new THREE.MeshStandardMaterial({ color: colors.Skin, roughness: 0.8 });
                    const matFace = new THREE.MeshStandardMaterial({ color: 0xffffff, map: texFace || null, roughness: 0.8 });
                    const headMats = [matSkinBase, matSkinBase, matSkinBase, matSkinBase, texFace ? matFace : matSkinBase, matSkinBase];
                    const matShirt = new THREE.MeshStandardMaterial({ color: texChest ? 0xffffff : colors.Shirt, map: texChest, roughness: 0.9 });
                    const matPants = new THREE.MeshStandardMaterial({ color: texLegs ? 0xffffff : colors.Pants, map: texLegs, roughness: 0.9 });

                    // Head
                    const head = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.5, 0.5), headMats);
                    head.position.y = 1.6;
                    head.castShadow = true;
                    group.add(head);

                    // Body
                    const body = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.75, 0.3), matShirt);
                    body.position.y = 0.975;
                    body.castShadow = true;
                    group.add(body);

                    // Arms
                    const armGeo = new THREE.BoxGeometry(0.2, 0.75, 0.2);
                    armGeo.translate(0, -0.3, 0);

                    const armL = new THREE.Mesh(armGeo, matShirt);
                    armL.position.set(0.42, 1.3, 0);
                    group.add(armL);

                    const armR = new THREE.Mesh(armGeo, matShirt);
                    armR.position.set(-0.42, 1.3, 0);
                    group.add(armR);

                    // Legs
                    const legGeo = new THREE.BoxGeometry(0.25, 0.6, 0.25);
                    legGeo.translate(0, -0.3, 0);

                    const legL = new THREE.Mesh(legGeo, matPants);
                    legL.position.set(0.15, 0.6, 0);
                    group.add(legL);

                    const legR = new THREE.Mesh(legGeo, matPants);
                    legR.position.set(-0.15, 0.6, 0);
                    group.add(legR);

                    group.userData.legs = { left: legL, right: legR };
                    group.userData.arms = { left: armL, right: armR };
                    group.userData.hands = { right: armR }; // Attachment point
                } else if (data.Parts) {
                    // Standard Procedural Prop (Weapons, Walls)
                    data.Parts.forEach(part => {
                        let geometry;
                        if (part.Shape === 'Sphere') geometry = new THREE.SphereGeometry(1, 16, 16);
                        else if (part.Shape === 'Cylinder') geometry = new THREE.CylinderGeometry(0.5, 0.5, 1, 16);
                        else geometry = new THREE.BoxGeometry(1, 1, 1);

                        const material = new THREE.MeshStandardMaterial({
                            color: part.ColorHex,
                            roughness: 0.6,
                            metalness: part.Material === 'Metal' ? 0.9 : 0.1,
                            emissive: part.Material === 'Glow' ? part.ColorHex : 0x000000
                        });
                        const mesh = new THREE.Mesh(geometry, material);
                        if (part.Position) mesh.position.set(part.Position[0], part.Position[1], part.Position[2]);
                        if (part.Rotation) mesh.rotation.set(part.Rotation[0] * Math.PI / 180, part.Rotation[1] * Math.PI / 180, part.Rotation[2] * Math.PI / 180);
                        if (part.Scale) mesh.scale.set(part.Scale[0], part.Scale[1], part.Scale[2]);
                        mesh.castShadow = true;
                        group.add(mesh);
                    });
                }
                group.scale.set(scale, scale, scale);
                return group;
            } catch (e) { console.error(e); return null; }
        }

        // --- DUNGEON BUILDER ---
        const dungeonParams = { w: 40, h: 40 };
        const floorGeo = new THREE.PlaneGeometry(dungeonParams.w, dungeonParams.h);
        // Stone Grey Floor
        const floorMat = new THREE.MeshStandardMaterial({ color: 0x555555, roughness: 0.9 });
        const floor = new THREE.Mesh(floorGeo, floorMat);
        floor.rotation.x = -Math.PI / 2;
        floor.receiveShadow = true;
        scene.add(floor);

        // Torches (Visual Only - No Light Pool)
        async function spawnTorch(x, z) {
            const torch = await loadProp('props/torch.json', 1.0);
            if (torch) {
                torch.position.set(x, 2, z);
                scene.add(torch);
                // No PointLight added here!
                torches.push({ mesh: torch, light: null, baseInt: 0, seed: Math.random() * 100 });
            }
        }
        await spawnTorch(-10, -10);
        await spawnTorch(10, 10);
        await spawnTorch(-10, 10);
        await spawnTorch(10, -10);
        await spawnTorch(0, 0);
        await spawnTorch(0, -15);
        await spawnTorch(0, 15);

        // Create simple Health Bar helper
        function createHealthBar() {
            const group = new THREE.Group();

            // Backing (Red/Black)
            const bg = new THREE.Mesh(
                new THREE.PlaneGeometry(1, 0.1),
                new THREE.MeshBasicMaterial({ color: 0x330000 })
            );
            group.add(bg);

            // Health (Green)
            const bar = new THREE.Mesh(
                new THREE.PlaneGeometry(1, 0.1),
                new THREE.MeshBasicMaterial({ color: 0x00ff00 })
            );
            bar.position.z = 0.01; // Slightly in front
            group.add(bar);

            return { group, bar };
        }

        // --- ENTITIES ---
        async function spawnEntity(type, x, z, team, weaponFile) {
            const ent = await loadProp('props/' + type + '.json', 0.55);
            if (!ent) return;

            ent.position.set(x, 0, z);
            scene.add(ent);

            // Health Bar
            const hb = createHealthBar();
            hb.group.position.y = 2.0; // Above head
            ent.add(hb.group);

            // Stats
            const maxHp = team === 'hero' ? 500 : 150;
            const data = {
                team: team,
                hp: maxHp,
                maxHp: maxHp,
                range: 1.5,
                state: 'IDLE',
                timer: 0,
                target: null,
                visual: ent,
                healthBar: hb
            };

            // MERGE data into existing userData to preserve hands/legs/arms
            Object.assign(ent.userData, data);

            // Link back for convenience
            data.mesh = ent;

            // Weapon equip
            if (weaponFile && ent.userData.hands) {
                const weapon = await loadProp('props/' + weaponFile, 1.0);
                if (weapon) {
                    weapon.rotation.x = Math.PI / 2;
                    weapon.position.y = -0.5;
                    ent.userData.hands.right.add(weapon);
                }
            }

            entities.push(data);
        }

        // Spawn Party
        await spawnEntity('hero', 0, 5, 'hero', 'sword.json');

        // Spawn Monsters
        await spawnEntity('orc', -8, -8, 'monster', 'axe.json');
        await spawnEntity('goblin', 8, -8, 'monster', 'mace.json');
        await spawnEntity('goblin', 0, -12, 'monster', 'mace.json');

        // Logic Loop
        function animate() {
            requestAnimationFrame(animate);
            if (controls) controls.update();
            const time = Date.now() * 0.001;

            // AI Logic
            entities.forEach(ent => {
                // Update Health Bar
                if (ent.healthBar) {
                    ent.healthBar.group.lookAt(camera.position);
                    const pct = Math.max(0, ent.hp / ent.maxHp);
                    ent.healthBar.bar.scale.x = pct;
                    ent.healthBar.bar.position.x = -0.5 * (1 - pct); // Align left
                }

                if (ent.hp <= 0) {
                    ent.mesh.rotation.x = -Math.PI / 2; // Dead
                    ent.mesh.position.y = 0.2;
                    if (ent.healthBar) ent.healthBar.group.visible = false;
                    return;
                }

                // Find Target
                if (!ent.target || ent.target.hp <= 0) {
                    // Scan for closest enemy
                    let closest = null;
                    let minDist = 999;
                    entities.forEach(other => {
                        if (other.team !== ent.team && other.hp > 0) {
                            const d = ent.mesh.position.distanceTo(other.mesh.position);
                            if (d < minDist) {
                                minDist = d;
                                closest = other;
                            }
                        }
                    });
                    ent.target = closest;
                    ent.state = 'IDLE';
                }

                if (!ent.target) return; // Victory/Peace

                const dist = ent.mesh.position.distanceTo(ent.target.mesh.position);

                if (dist > ent.range) {
                    // CHASE
                    const dir = new THREE.Vector3().subVectors(ent.target.mesh.position, ent.mesh.position).normalize();
                    ent.mesh.position.addScaledVector(dir, 0.03);
                    ent.mesh.lookAt(ent.target.mesh.position.x, 0, ent.target.mesh.position.z);

                    // Walk Anim
                    if (ent.mesh.userData.legs) {
                        const t = time * 8;
                        ent.mesh.userData.legs.left.rotation.x = Math.sin(t) * 0.6;
                        ent.mesh.userData.legs.right.rotation.x = Math.sin(t + Math.PI) * 0.6;
                        if (ent.mesh.userData.arms) {
                            ent.mesh.userData.arms.left.rotation.x = Math.sin(t + Math.PI) * 0.6;
                            ent.mesh.userData.arms.right.rotation.x = Math.sin(t) * 0.6;
                        }
                    }
                } else {
                    // ATTACK
                    ent.timer -= 0.016;
                    if (ent.timer <= 0) {
                        ent.timer = 1.0; // Attack cooldown

                        // Swing Anim Trigger
                        const arm = ent.mesh.userData.arms ? ent.mesh.userData.arms.right : null;
                        if (arm) {
                            ent.isAttacking = true;
                            setTimeout(() => { ent.isAttacking = false; }, 200);
                        }

                        // Damage
                        if (Math.random() > 0.3) {
                            const victim = ent.target;

                            // Base damage
                            let dmg = 2 + Math.floor(Math.random() * 4);
                            // HERO BUFF
                            if (ent.team === 'hero') dmg *= 3;

                            victim.hp -= dmg;

                            // Flash red
                            if (victim.mesh && victim.mesh.children) {
                                victim.mesh.children.forEach(c => {
                                    if (c.material && c.material.emissive) c.material.emissive.setHex(0xff0000);
                                });
                                setTimeout(() => {
                                    if (victim.mesh && victim.mesh.children) {
                                        victim.mesh.children.forEach(c => {
                                            if (c.material && c.material.emissive) c.material.emissive.setHex(0x000000);
                                        });
                                    }
                                }, 100);
                            }
                        }
                    }
                }

                // Swing Animation Logic
                if (ent.isAttacking && ent.mesh.userData.arms) {
                    ent.mesh.userData.arms.right.rotation.x = -1.5; // Raise arm
                } else if (!ent.isAttacking && dist <= ent.range && ent.mesh.userData.arms) {
                    ent.mesh.userData.arms.right.rotation.x = 0; // Reset
                }

            });

            renderer.render(scene, camera);
        }
        animate();
    }
};
