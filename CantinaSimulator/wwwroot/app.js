window.CantinaSimulator = {
    init: async function (containerId) {
        console.log("Initializing CantinaSimulator Final No Hopping...");

        const container = document.getElementById(containerId);
        if (!container) return;
        while (container.firstChild) container.removeChild(container.firstChild);

        // Vars
        let scene, camera, renderer, controls;
        let npcs = [];

        // Scene
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x101010);
        scene.fog = new THREE.Fog(0x101010, 10, 60);

        // --- AUDIO ---
        let audioContext = null;
        let isPlaying = false;

        function playNote(freq, dur, type = 'sawtooth') {
            if (!audioContext) return;
            const osc = audioContext.createOscillator();
            const gain = audioContext.createGain();
            osc.type = type;
            osc.frequency.setValueAtTime(freq, audioContext.currentTime);
            osc.connect(gain);
            gain.connect(audioContext.destination);

            osc.start();
            gain.gain.setValueAtTime(0.1, audioContext.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, audioContext.currentTime + dur);
            osc.stop(audioContext.currentTime + dur);
        }

        function startMusic() {
            if (isPlaying) return;
            isPlaying = true;
            audioContext = new (window.AudioContext || window.webkitAudioContext)();

            // Cantina Band (Simplified) - Key of Dm?
            const tempo = 260;
            const noteDur = 60 / tempo;

            const notes = [
                // A, D, A, D, A, D, A...
                { f: 440, d: 0.25 }, { f: 587, d: 0.25 },
                { f: 440, d: 0.25 }, { f: 587, d: 0.25 },
                { f: 440, d: 0.25 }, { f: 587, d: 0.25 },
                { f: 440, d: 0.25 }, { f: 415, d: 0.25 }, // Ab
                { f: 440, d: 0.25 }, { f: 587, d: 0.25 },
                { f: 440, d: 0.25 }, { f: 415, d: 0.25 }, // Ab
                { f: 392, d: 0.25 }, // G
                { f: 392, d: 0.25 }, { f: 587, d: 0.25 }, // G, D
                { f: 392, d: 0.25 }, { f: 659, d: 0.25 }, // G, E
                { f: 622, d: 0.25 }, { f: 587, d: 0.25 }, // Eb, D
                { f: 523, d: 0.25 }, { f: 466, d: 0.25 }, // C, Bb
                { f: 440, d: 0.5 }, // A
            ];

            let noteIdx = 0;
            setInterval(() => {
                if (audioContext.state === 'suspended') audioContext.resume();
                const n = notes[noteIdx];
                // bass
                if (noteIdx % 4 === 0) playNote(146, 0.1, 'square'); // D3 bass

                playNote(n.f, n.d);
                noteIdx = (noteIdx + 1) % notes.length;
            }, noteDur * 1000);
        }

        // Click to start
        const overlay = document.createElement('div');
        overlay.style.position = 'absolute';
        overlay.style.top = '10px';
        overlay.style.left = '10px';
        overlay.style.color = '#ffaa00';
        overlay.style.fontFamily = 'Courier New';
        overlay.style.cursor = 'pointer';
        overlay.style.padding = '10px';
        overlay.style.border = '1px solid #ffaa00';
        overlay.style.backgroundColor = 'rgba(0,0,0,0.7)';
        overlay.innerText = "[ CLICK TO START MUSIC ]";
        container.appendChild(overlay);

        container.addEventListener('click', () => {
            startMusic();
            overlay.innerText = "Playing: 'Mad About Me' (Cantina Band)";
            overlay.style.opacity = '0.5';
        });

        camera = new THREE.PerspectiveCamera(60, container.clientWidth / container.clientHeight, 0.1, 1000);
        camera.position.set(0, 30, 40);

        renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(container.clientWidth, container.clientHeight);
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        container.appendChild(renderer.domElement);

        // Env
        const ambientLight = new THREE.AmbientLight(0xffaa55, 0.8);
        scene.add(ambientLight);
        const overheadLight = new THREE.HemisphereLight(0xffffff, 0x444444, 0.6);
        scene.add(overheadLight);

        // Controls
        if (typeof THREE.OrbitControls !== 'undefined') {
            controls = new THREE.OrbitControls(camera, renderer.domElement);
            controls.target.set(0, 0, 0);
            controls.update();
        } else {
            console.error("THREE.OrbitControls is missing!");
        }

        // Load Prop Helper
        async function loadProp(url, scale = 1.0) {
            try {
                const response = await fetch(url);
                const data = await response.json();
                const group = new THREE.Group();

                // Voxel / Humanoid Loader
                if (data.Type === 'Voxel') {
                    const colors = data.ProceduralColors || { Skin: 0x885533, Shirt: 0x335588, Pants: 0x222222 };

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

                    const texFace = data.Textures ? makeTex(data.Textures.Face) : null;
                    const texChest = data.Textures ? makeTex(data.Textures.Chest) : null;
                    const texLegs = data.Textures ? makeTex(data.Textures.Legs) : null;

                    // Materials
                    const matSkinBase = new THREE.MeshStandardMaterial({ color: colors.Skin, roughness: 0.8 });
                    const matFace = new THREE.MeshStandardMaterial({
                        color: 0xffffff,
                        map: texFace || null,
                        roughness: 0.8
                    });
                    // Face on Z+ (index 4)
                    const headMats = [matSkinBase, matSkinBase, matSkinBase, matSkinBase, texFace ? matFace : matSkinBase, matSkinBase];

                    const matShirt = new THREE.MeshStandardMaterial({
                        color: texChest ? 0xffffff : colors.Shirt,
                        map: texChest,
                        roughness: 0.9
                    });

                    const matPants = new THREE.MeshStandardMaterial({
                        color: texLegs ? 0xffffff : colors.Pants,
                        map: texLegs,
                        roughness: 0.9
                    });

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
                    armGeo.translate(0, -0.3, 0); // pivot near shoulder

                    const armL = new THREE.Mesh(armGeo, matShirt);
                    armL.position.set(0.42, 1.3, 0);
                    group.add(armL);

                    const armR = new THREE.Mesh(armGeo, matShirt);
                    armR.position.set(-0.42, 1.3, 0);
                    group.add(armR);

                    // Legs
                    const legGeo = new THREE.BoxGeometry(0.25, 0.6, 0.25);
                    legGeo.translate(0, -0.3, 0); // Pivot at hip

                    const legL = new THREE.Mesh(legGeo, matPants);
                    legL.position.set(0.15, 0.6, 0);
                    group.add(legL);

                    const legR = new THREE.Mesh(legGeo, matPants);
                    legR.position.set(-0.15, 0.6, 0);
                    group.add(legR);

                    group.userData.legs = { left: legL, right: legR };
                    group.userData.arms = { left: armL, right: armR };
                }

                // Procedural Parts Loader
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
                            roughness: 0.6,
                            metalness: part.Material === 'Metal' ? 0.8 : 0.1,
                            emissive: part.Material === 'Glow' ? part.ColorHex : 0x000000,
                            emissiveIntensity: 0.5
                        });

                        const mesh = new THREE.Mesh(geometry, material);
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
            } catch (e) { console.error(e); return null; }
        }

        // --- ROOM BUILDER ---
        const roomGroup = new THREE.Group();
        scene.add(roomGroup);

        // Floor
        const floorGeo = new THREE.PlaneGeometry(60, 60);
        const floorMat = new THREE.MeshStandardMaterial({ color: 0x332211, roughness: 0.8 });
        const floor = new THREE.Mesh(floorGeo, floorMat);
        floor.rotation.x = -Math.PI / 2;
        floor.receiveShadow = true;
        roomGroup.add(floor);

        // Walls
        function makeWall(x, z, w, d, h) {
            const wall = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshStandardMaterial({ color: 0x443322 }));
            wall.position.set(x, h / 2, z);
            wall.receiveShadow = true;
            wall.castShadow = true;
            roomGroup.add(wall);
        }
        makeWall(0, -30, 60, 2, 10); // Back
        makeWall(-30, 0, 2, 60, 10); // Left
        makeWall(30, 0, 2, 60, 10); // Right

        // Furniture
        const barCounter = await loadProp('props/bar.json', 1.0);
        if (barCounter) {
            for (let x = -10; x <= 10; x += 5) {
                const b = barCounter.clone();
                b.position.set(x, 0, -20);
                roomGroup.add(b);
            }
        }

        const tableProp = await loadProp('props/table.json', 1.0);
        const stoolProp = await loadProp('props/stool.json', 1.0);

        if (tableProp) {
            const positions = [[-15, 0], [15, 0], [-15, 15], [15, 15]];
            positions.forEach(pos => {
                const t = tableProp.clone();
                t.position.set(pos[0], 0, pos[1]);
                roomGroup.add(t);

                const pl = new THREE.PointLight(0xffaa00, 1, 15);
                pl.position.set(pos[0], 5, pos[1]);
                pl.castShadow = true;
                scene.add(pl);

                if (stoolProp) {
                    for (let i = 0; i < 4; i++) {
                        const s = stoolProp.clone();
                        const angle = i * (Math.PI / 2);
                        s.position.set(pos[0] + Math.cos(angle) * 3, 0, pos[1] + Math.sin(angle) * 3);
                        s.lookAt(pos[0], 0, pos[1]);
                        roomGroup.add(s);
                    }
                }
            });
        }

        const lamp = await loadProp('props/lamp.json', 1.0);
        if (lamp) {
            const l1 = lamp.clone(); l1.position.set(-25, 0, -25); roomGroup.add(l1);
            const l2 = lamp.clone(); l2.position.set(25, 0, -25); roomGroup.add(l2);
        }
        const bookshelf = await loadProp('props/bookshelf.json', 1.0);
        if (bookshelf) {
            bookshelf.position.set(-28, 0, -10);
            bookshelf.rotation.y = Math.PI / 2;
            roomGroup.add(bookshelf);
        }

        // --- NPCs ---
        const npcTypes = ['wookiee.json', 'customer_1.json', 'customer_2.json', 'customer_3.json'];

        async function spawnNPC(file, x, z, fixed = false) {
            const npc = await loadProp('props/' + file, 0.55); // Slightly larger
            if (!npc) return;

            npc.position.set(x, 0, z);
            npc.position.set(x, 0, z);

            // Merge into existing userData (legs/arms) instead of overwriting
            Object.assign(npc.userData, {
                fixed: fixed,
                state: 'IDLE',
                timer: Math.random() * 2,
                target: new THREE.Vector3()
            });
            scene.add(npc);
            npcs.push(npc);
            return npc;
        }

        await spawnNPC('bartender.json', 0, -23, true);
        await spawnNPC('bouncer.json', 0, 25, true);

        for (let i = 0; i < 8; i++) {
            const type = npcTypes[Math.floor(Math.random() * npcTypes.length)];
            await spawnNPC(type, (Math.random() - 0.5) * 40, (Math.random() - 0.5) * 30);
        }

        // Logic Loop
        function animate() {
            requestAnimationFrame(animate);
            if (controls) controls.update();
            const delta = 0.05;

            npcs.forEach(npc => {
                if (npc.userData.fixed) return;

                const data = npc.userData;
                data.timer -= delta;

                if (data.state === 'IDLE') {
                    if (data.timer <= 0) {
                        data.state = 'WALK';
                        data.target.set((Math.random() - 0.5) * 40, 0, (Math.random() - 0.5) * 40);
                        data.timer = 50 + Math.random() * 50;
                        npc.lookAt(data.target.x, 0, data.target.z);
                    }

                    // Reset legs
                    if (npc.userData.legs) {
                        const lerp = 0.1;
                        npc.userData.legs.left.rotation.x += (0 - npc.userData.legs.left.rotation.x) * lerp;
                        npc.userData.legs.right.rotation.x += (0 - npc.userData.legs.right.rotation.x) * lerp;

                        if (npc.userData.arms) {
                            npc.userData.arms.left.rotation.x += (0 - npc.userData.arms.left.rotation.x) * lerp;
                            npc.userData.arms.right.rotation.x += (0 - npc.userData.arms.right.rotation.x) * lerp;
                        }
                    }

                } else if (data.state === 'WALK') {
                    const dir = new THREE.Vector3().subVectors(data.target, npc.position);
                    const dist = dir.length();

                    if (dist < 1.0 || data.timer <= 0) {
                        data.state = 'IDLE';
                        data.timer = 2 + Math.random() * 3;
                    } else {
                        dir.normalize();
                        npc.position.addScaledVector(dir, 0.025); // Slower walk speed (was 0.05)

                        // Walk Animation
                        if (npc.userData.legs) {
                            const time = Date.now() * 0.003; // Slower animation cycle

                            // Legs Swing
                            npc.userData.legs.left.rotation.x = Math.sin(time) * 0.5;
                            npc.userData.legs.right.rotation.x = Math.sin(time + Math.PI) * 0.5;

                            // Arms Swing
                            if (npc.userData.arms) {
                                npc.userData.arms.left.rotation.x = Math.sin(time + Math.PI) * 0.5;
                                npc.userData.arms.right.rotation.x = Math.sin(time) * 0.5;
                            }

                            // NO BOB
                            npc.position.y = 0;

                        } else {
                            npc.position.y = 0;
                        }
                    }
                }
            });

            renderer.render(scene, camera);
        }
        animate();
    }
};
