const debugConsole = document.getElementById("debugConsole");
if (debugConsole) {
    debugConsole.style.display = "none";
    debugConsole.style.position = "absolute";
    debugConsole.style.top = "0";
    debugConsole.style.zIndex = "100";
    debugConsole.style.color = "lime";
}

// FORCE FULLSCREEN LAYOUT (Injected Fix)
document.documentElement.style.width = "100%";
document.documentElement.style.height = "100%";
document.documentElement.style.margin = "0";
document.documentElement.style.padding = "0";
document.body.style.width = "100%";
document.body.style.height = "100%";
document.body.style.margin = "0";
document.body.style.padding = "0";
document.body.style.overflow = "hidden";

const cvs = document.getElementById("renderCanvas");
if (cvs) {
    cvs.style.position = "absolute";
    cvs.style.top = "0";
    cvs.style.left = "0";
    cvs.style.width = "100%";
    cvs.style.height = "100%";
    cvs.style.display = "block";
    cvs.style.zIndex = "1";
}

function log(msg) {
    const div = document.createElement("div");
    div.innerText = "[" + new Date().toLocaleTimeString() + "] " + msg;
    debugConsole.appendChild(div);
    debugConsole.scrollTop = debugConsole.scrollHeight;
    console.log(msg);
}
window.onerror = (m, s, l) => log("ERR: " + m + " at " + s + ":" + l);
const canvas = document.getElementById("renderCanvas");
canvas.oncontextmenu = (e) => e.preventDefault();
const engine = new BABYLON.Engine(canvas, true);
let shadowGenerator;
let playerRoot = null;
let sun, hemi, ground, groundMat; // Exposed for updates
let gridMesh, gridMat; // New for alignment

const createScene = function () {
    const scene = new BABYLON.Scene(engine);

    // Sky
    scene.clearColor = new BABYLON.Color4(0.53, 0.81, 0.92, 1);
    scene.ambientColor = new BABYLON.Color3(0.2, 0.2, 0.3);

    var env = scene.createDefaultEnvironment({
        createSkybox: false,
        createGround: false,
        setupImageProcessing: true,
        environmentTexture: "https://assets.babylonjs.com/environments/studio.env"
    });
    scene.environmentIntensity = 0.8;
    // Fog Removed by User Request
    // scene.fogMode = BABYLON.Scene.FOGMODE_EXP;
    // scene.fogDensity = 0.01;
    // scene.fogColor = new BABYLON.Color3(0.53, 0.81, 0.92);

    // Camera (Follow)
    const camera = new BABYLON.ArcRotateCamera("camera", -Math.PI / 2, Math.PI / 2.2, 8, new BABYLON.Vector3(0, 1, 0), scene);
    camera.attachControl(canvas, true);
    camera.lowerRadiusLimit = 2;
    camera.upperRadiusLimit = 200;
    camera.wheelPrecision = 20;
    camera.panningSensibility = 1000; camera.panningMouseButton = 2;    // Explicitly set Right-Click (2)

    // Lighting
    sun = new BABYLON.DirectionalLight("sun", new BABYLON.Vector3(-1, -2, -1), scene);
    sun.position = new BABYLON.Vector3(50, 100, 50);
    sun.intensity = 1.0;

    hemi = new BABYLON.HemisphericLight("hemi", new BABYLON.Vector3(0, 1, 0), scene);
    hemi.intensity = 0.6;

    shadowGenerator = new BABYLON.ShadowGenerator(2048, sun);
    shadowGenerator.useBlurExponentialShadowMap = true;
    shadowGenerator.blurKernel = 32;
    shadowGenerator.setDarkness(0.4);

    // Ground
    ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 500, height: 500 }, scene);
    groundMat = new BABYLON.StandardMaterial("gMat", scene);
    groundMat.diffuseColor = new BABYLON.Color3(0.4, 0.25, 0.15); // Wood Brown
    groundMat.specularColor = new BABYLON.Color3(0, 0, 0);

    // Wood Texture
    const wood = new BABYLON.Texture("https://assets.babylonjs.com/textures/wood.jpg", scene);
    wood.uScale = 10;
    wood.vScale = 10;
    groundMat.diffuseTexture = wood;

    ground.material = groundMat;
    ground.receiveShadows = true;

    // Proper Alignment Grid (Procedural)
    gridMesh = BABYLON.MeshBuilder.CreateGround("gridOverlay", { width: 500, height: 500 }, scene);
    gridMesh.position.y = 0.01; // Slightly above ground
    gridMat = new BABYLON.GridMaterial("gridMat", scene);
    gridMat.gridRatio = 3.0; // 3 units = 1 big block (per user request)
    gridMat.mainColor = new BABYLON.Color3(1, 1, 1);
    gridMat.lineColor = new BABYLON.Color3(0.5, 0.5, 1.0);
    gridMat.opacity = 0.5;
    gridMat.backFaceCulling = false;

    gridMesh.material = gridMat;
    gridMesh.setEnabled(false); // Default OFF

    const meshMap = new Map();

    // INPUT
    const input = { w: false, s: false, a: false, d: false };
    window.addEventListener("keydown", (e) => {
        switch (e.key.toLowerCase()) {
            case "w": input.w = true; break;
            case "s": input.s = true; break;
            case "a": input.a = true; break;
            case "d": input.d = true; break;
        }
    });
    window.addEventListener("keyup", (e) => {
        switch (e.key.toLowerCase()) {
            case "w": input.w = false; break;
            case "s": input.s = false; break;
            case "a": input.a = false; break;
            case "d": input.d = false; break;
        }
    });

    window.addEventListener("keydown", (e) => {
        if (e.key.toLowerCase() === "f" && playerRoot) {
            camera.setTarget(new BABYLON.Vector3(playerRoot.position.x, playerRoot.position.y + 1, playerRoot.position.z));
        }
    });

    // GIZMOS (Manipulation)
    const gizmoManager = new BABYLON.GizmoManager(scene);
    gizmoManager.positionGizmoEnabled = true;
    gizmoManager.usePointerToAttachGizmos = false; // Custom logic

    // Fix for camera fight (Removed detachment to allow panning while selected)
    gizmoManager.onAttachedToMeshObservable.add((mesh) => {
        // If we detach camera, we can't pan. 
        // Babylon Gizmos handle interaction well enough without detachment.
    });

    scene.onPointerObservable.add((pointerInfo) => {
        const evt = pointerInfo.event;
        if (pointerInfo.type === BABYLON.PointerEventTypes.POINTERDOWN) {
            if (evt.button !== 0) return; // ONLY Left Click for selection

            const pickInfo = pointerInfo.pickInfo;
            if (pickInfo.hit && pickInfo.pickedMesh && pickInfo.pickedMesh !== ground && pickInfo.pickedMesh !== gridMesh) {
                let target = pickInfo.pickedMesh;
                let p = target.parent;
                while (p && p.id && !p.id.startsWith("recipe_") && p.id !== "root") {
                    p = p.parent;
                }
                if (p && (p.id.startsWith("recipe_") || p.id === "root")) target = p;

                // Only attach gizmos if NOT in drag or none mode
                const currentMode = window.MCTwinGizmos.mode;
                if (currentMode === 'move' || currentMode === 'rotate' || currentMode === 'scale') {
                    gizmoManager.attachToNode(target);
                } else {
                    gizmoManager.attachToNode(null);
                }

                // Send Selection Event
                if (window.chrome && window.chrome.webview) {
                    console.log("DEBUG: Sending Selection Event for " + target.id);
                    window.chrome.webview.postMessage({
                        type: 'selection',
                        data: {
                            id: target.id,
                            recipeName: target.metadata ? target.metadata.recipeName : "Unknown",
                            tags: target.metadata ? (target.metadata.tags || {}) : {}
                        }
                    });
                } else {
                    console.log("DEBUG: window.chrome.webview NOT FOUND");
                }
            } else {
                if (!evt.shiftKey) gizmoManager.attachToNode(null);
            }
        }
    });

    // Global access for UI
    window.MCTwinGizmos = {
        mode: 'move',
        setupDrag: function (node) {
            const meshes = node.getChildMeshes();
            meshes.forEach(m => {
                if (m.metadata && m.metadata.drag) m.removeBehavior(m.metadata.drag);

                const drag = new BABYLON.PointerDragBehavior({ dragPlaneNormal: new BABYLON.Vector3(0, 1, 0) });
                drag.moveAttached = false; // CRITICAL: Stop the child mesh from moving independently
                drag.useObjectOrientationForDragging = false;
                drag.onDragObservable.add((event) => {
                    node.position.addInPlace(event.delta);
                });
                m.metadata = m.metadata || {};
                m.metadata.drag = drag;
                m.addBehavior(drag);
                drag.enabled = (this.mode === 'drag');
            });
        },
        setMode: function (m) {
            this.mode = m;
            gizmoManager.positionGizmoEnabled = (m === 'move');
            gizmoManager.rotationGizmoEnabled = (m === 'rotate');
            gizmoManager.scaleGizmoEnabled = (m === 'scale');
            gizmoManager.attachToNode(null);

            scene.meshes.forEach(mesh => {
                if (mesh.metadata && mesh.metadata.drag) {
                    mesh.metadata.drag.enabled = (m === 'drag');
                }
            });
        }
    };

    // ANIMATION & MOVEMENT
    let animTime = 0;
    let isMoving = false;
    let isGlobalAnimating = false;

    scene.registerBeforeRender(function () {
        const dt = engine.getDeltaTime() / 1000.0;
        animTime += dt;

        // 1. Player Movement (Manual)
        if (playerRoot) {
            const speed = 2.5 * dt;
            isMoving = false;

            const turnSpeed = 3.0 * dt;
            if (input.a) playerRoot.rotation.y -= turnSpeed;
            if (input.d) playerRoot.rotation.y += turnSpeed;

            const forward = new BABYLON.Vector3(Math.sin(playerRoot.rotation.y), 0, Math.cos(playerRoot.rotation.y));

            if (input.w) {
                playerRoot.position.addInPlace(forward.scale(speed));
                isMoving = true;
            }
            if (input.s) {
                playerRoot.position.subtractInPlace(forward.scale(speed * 0.6));
                isMoving = true;
            }

            if (isMoving) {
                const desiredTarget = new BABYLON.Vector3(playerRoot.position.x, playerRoot.position.y + 1, playerRoot.position.z);
                camera.target = BABYLON.Vector3.Lerp(camera.target, desiredTarget, 0.1);
            }

            animateVoxelNodes(playerRoot, isMoving, animTime);
        }

        // 2. Global AI Animation (NPCs & Props)
        if (isGlobalAnimating) {
            scene.transformNodes.forEach(node => {
                if (node === playerRoot) return;
                if (!node.id || (!node.id.startsWith("recipe_") && node.id !== "root")) return;

                const meta = node.metadata || {};

                // --- NPC Random Walk ---
                if (meta.artType === "Voxel") {
                    if (!meta.ai) {
                        meta.ai = {
                            targetPos: node.position.clone(),
                            state: 'idle',
                            timer: Math.random() * 2
                        };
                    }

                    const ai = meta.ai;
                    ai.timer -= dt;

                    if (ai.state === 'idle' && ai.timer <= 0) {
                        // Pick new target
                        ai.state = 'moving';
                        ai.targetPos = node.position.add(new BABYLON.Vector3((Math.random() - 0.5) * 10, 0, (Math.random() - 0.5) * 10));
                        ai.timer = 5 + Math.random() * 5;
                    } else if (ai.state === 'moving') {
                        const dist = BABYLON.Vector3.Distance(node.position, ai.targetPos);
                        if (dist < 0.5 || ai.timer <= 0) {
                            ai.state = 'idle';
                            ai.timer = 2 + Math.random() * 3;
                        } else {
                            // Move & Rotate towards target
                            const dir = ai.targetPos.subtract(node.position).normalize();
                            node.position.addInPlace(dir.scale(1.5 * dt));

                            const targetRot = Math.atan2(dir.x, dir.z);
                            let diff = targetRot - node.rotation.y;
                            while (diff < -Math.PI) diff += Math.PI * 2;
                            while (diff > Math.PI) diff -= Math.PI * 2;
                            node.rotation.y += diff * 0.1;

                            animateVoxelNodes(node, true, animTime);
                        }
                    }

                    if (ai.state === 'idle') {
                        animateVoxelNodes(node, false, animTime);
                    }
                }

                // --- Prop Animation Looping ---
                const propTimeline = meta.recipeData ? (meta.recipeData.Timeline || meta.recipeData.AnimationSequences) : null;
                if (meta.artType !== "Voxel" && propTimeline) {
                    if (meta.animTimer === undefined) meta.animTimer = 0;
                    meta.animTimer += dt;

                    // Simple loop based on timeline duration
                    // Calculate max duration in timeline
                    let maxDuration = 2.0;
                    if (Array.isArray(propTimeline)) {
                        propTimeline.forEach(ev => {
                            const d = (ev.Time || 0) + (ev.Duration || 1);
                            if (d > maxDuration) maxDuration = d;
                        });
                    }

                    if (meta.animTimer > maxDuration) {
                        meta.animTimer = 0;
                        window.MCTwin.runTimeline(propTimeline, meta.instanceRegistry);
                    }
                }
            });
        }
    });

    function animateVoxelNodes(root, moving, time) {
        const map = new Map();
        root.getChildMeshes().forEach(m => {
            const name = m.name.split('_')[0];
            map.set(name, m);
        });

        const legR = map.get("RightLeg");
        const legL = map.get("LeftLeg");
        const armR = map.get("RightArm");
        const armL = map.get("LeftArm");

        if (moving) {
            const wSpeed = 10;
            const phase = Math.sin(time * wSpeed);
            if (legR) legR.rotation.x = phase * 0.8;
            if (legL) legL.rotation.x = -phase * 0.8;
            if (armR) armR.rotation.x = -phase * 0.8;
            if (armL) armL.rotation.x = phase * 0.8;
        } else {
            [legR, legL, armR, armL].forEach(m => { if (m) m.rotation.x = 0; });
            if (armR) armR.rotation.z = 0.05 + Math.sin(time) * 0.05;
            if (armL) armL.rotation.z = -0.05 - Math.sin(time) * 0.05;
        }
    }


    // --- NEXUS BRIDGE INTEGRATION ---
    const parseVec3 = (data, defaultVal = { x: 0, y: 0, z: 0 }) => {
        if (!data) return new BABYLON.Vector3(defaultVal.x, defaultVal.y, defaultVal.z);
        if (Array.isArray(data)) return new BABYLON.Vector3(data[0] ?? defaultVal.x, data[1] ?? defaultVal.y, data[2] ?? defaultVal.z);
        if (typeof data === 'object') return new BABYLON.Vector3(data.x ?? data.X ?? defaultVal.x, data.y ?? data.Y ?? defaultVal.y, data.z ?? data.Z ?? defaultVal.z);
        return new BABYLON.Vector3(defaultVal.x, defaultVal.y, defaultVal.z);
    };

    const createMaterial = (id, config) => {
        const mat = new BABYLON.PBRMaterial("mat_" + id, scene);
        let color = new BABYLON.Color3(0.5, 0.5, 0.5);
        if (config.ColorHex) color = BABYLON.Color3.FromHexString(config.ColorHex);

        mat.albedoColor = color;
        mat.reflectivityColor = new BABYLON.Color3(0.1, 0.1, 0.1);
        mat.microSurface = 0.9; // Smooth by default

        const type = (config.Material || "Plastic").toLowerCase();

        if (type.includes("metal") || type.includes("chrome")) {
            mat.metallic = 1.0;
            mat.roughness = 0.1;
            mat.reflectivityColor = new BABYLON.Color3(0.9, 0.9, 0.9);
        } else if (type.includes("glass") || type.includes("crystal")) {
            mat.metallic = 0.2;
            mat.roughness = 0.05;
            mat.alpha = 0.4;
            mat.transparencyMode = BABYLON.PBRMaterial.PBRMATERIAL_ALPHABLEND;
            // Fresnel-like effect
            mat.indexOfRefraction = 1.5;
            mat.directIntensity = 1.2;
        } else if (type.includes("glow") || type.includes("neon")) {
            mat.emissiveColor = color;
            mat.emissiveIntensity = 2.0;
            mat.disableLighting = false; // Allow it to catch shadows/specular
        } else {
            mat.metallic = 0.0;
            mat.roughness = 0.5;
        }

        // Use environment reflections if available
        mat.reflectionTexture = scene.environmentTexture;

        return mat;
    };

    window.MCTwin = {
        clear: function () {
            // Dispose only character if we want items to persist, 
            // BUT for Scene loading, we might want a full clear.
            if (playerRoot) { playerRoot.dispose(); playerRoot = null; }
            meshMap.clear();
        },

        clearAll: function () {
            this.clear();
            scene.transformNodes.slice().forEach(n => {
                if (n.id && n.id.indexOf("recipe_") !== -1) n.dispose();
            });
        },

        propRegistry: {},
        spawnProp: function (config, parentNode = null, registry = null) {
            const id = config.Id || "prop_" + Date.now();
            const targetRegistry = registry || this.propRegistry;

            let mesh;
            const shape = (config.Shape || "Box").toLowerCase();
            const scale = parseVec3(config.Scale, { x: 1, y: 1, z: 1 });

            try {
                switch (shape) {
                    case "sphere": mesh = BABYLON.MeshBuilder.CreateSphere("prop_" + id, { diameter: 1, segments: 16 }, scene); break;
                    case "cylinder": mesh = BABYLON.MeshBuilder.CreateCylinder("prop_" + id, { diameter: 1, height: 1, tessellation: 32 }, scene); break;
                    case "plane": mesh = BABYLON.MeshBuilder.CreatePlane("prop_" + id, { size: 1 }, scene); mesh.rotation.x = Math.PI / 2; break;
                    case "torus": mesh = BABYLON.MeshBuilder.CreateTorus("prop_" + id, { diameter: 1, thickness: 0.2, tessellation: 32 }, scene); break;
                    case "capsule": mesh = BABYLON.MeshBuilder.CreateCapsule("prop_" + id, { radius: 0.5, height: 2, tessellation: 32 }, scene); break;
                    default: mesh = BABYLON.MeshBuilder.CreateBox("prop_" + id, { size: 1 }, scene);
                }
            } catch (e) { mesh = BABYLON.MeshBuilder.CreateBox("prop_" + id, { size: 1 }, scene); }

            mesh.scaling = scale;
            targetRegistry[id] = mesh;

            if (config.ParentId && targetRegistry[config.ParentId]) {
                mesh.parent = targetRegistry[config.ParentId];
            } else if (parentNode) {
                mesh.parent = parentNode;
            }

            const rawPos = parseVec3(config.Position);
            const rawScale = scale;
            mesh.intendedScale = rawScale;

            if (config.ParentId && targetRegistry[config.ParentId]) {
                const parentProp = targetRegistry[config.ParentId];
                const ps = parentProp.intendedScale || new BABYLON.Vector3(1, 1, 1);
                // Compensate for immediate parent's non-uniform scaling
                mesh.scaling = new BABYLON.Vector3(rawScale.x / ps.x, rawScale.y / ps.y, rawScale.z / ps.z);
                mesh.position = new BABYLON.Vector3(rawPos.x / ps.x, rawPos.y / ps.y, rawPos.z / ps.z);
            } else {
                // Root part of the recipe or unparented prop
                mesh.scaling = rawScale;
                mesh.position = rawPos;
            }

            const rot = parseVec3(config.Rotation);
            mesh.rotation = new BABYLON.Vector3(BABYLON.Tools.ToRadians(rot.x), BABYLON.Tools.ToRadians(rot.y), BABYLON.Tools.ToRadians(rot.z));

            mesh.material = createMaterial(id, config);
            shadowGenerator.addShadowCaster(mesh);
            mesh.receiveShadows = true;

            return id;
        },

        spawnRecipe: function (json, recipeName = "Unknown", isSelectable = false, transform = null) {
            try {
                let data = json;
                if (typeof json === 'string') { try { data = JSON.parse(json); } catch (e) { log("ERR: JSON Parse failed: " + e.message); return; } }

                const recipeId = data.Id || "recipe_" + Date.now();
                const container = new BABYLON.TransformNode(recipeId, scene);
                const instanceRegistry = {};
                container.metadata = { recipeName: recipeName, artType: "Procedural", recipeData: data, instanceRegistry: instanceRegistry };

                // Minecraft scaling: 16 units = 1 block.
                const globalScaleMultiplier = 1.0;
                container.scaling = new BABYLON.Vector3(globalScaleMultiplier, globalScaleMultiplier, globalScaleMultiplier);

                if (transform) {
                    const pos = transform.Position || transform.position;
                    const rot = transform.Rotation || transform.rotation;
                    const scl = transform.Scale || transform.scale;

                    if (pos) container.position = new BABYLON.Vector3(pos[0], pos[1], pos[2]);
                    if (rot) container.rotation = new BABYLON.Vector3(BABYLON.Tools.ToRadians(rot[0]), BABYLON.Tools.ToRadians(rot[1]), BABYLON.Tools.ToRadians(rot[2]));
                    if (scl) container.scaling = new BABYLON.Vector3(scl[0], scl[1], scl[2]);
                    if (transform.Tags) container.metadata.tags = transform.Tags;
                } else if (playerRoot) {
                    const forward = new BABYLON.Vector3(Math.sin(playerRoot.rotation.y), 0, Math.cos(playerRoot.rotation.y));
                    const spawnPos = playerRoot.position.add(forward.scale(globalScaleMultiplier * 4));
                    spawnPos.y = 0;
                    container.position = spawnPos;
                }

                if (data.Parts && Array.isArray(data.Parts)) {
                    log("Spawning Recipe: " + data.Name + " (ArtType: " + (transform ? transform.ArtType : "New") + ")");

                    // 1. Spawn All under container
                    data.Parts.forEach(p => {
                        this.spawnProp(p, container, instanceRegistry);
                    });

                    // 2. Re-Parent within recipe
                    data.Parts.forEach(p => {
                        if (p.ParentId && instanceRegistry[p.ParentId] && instanceRegistry[p.Id]) {
                            instanceRegistry[p.Id].parent = instanceRegistry[p.ParentId];
                        }
                    });

                    // 3. Auto-Grounding (ONLY for new spawns without manual positioning)
                    if (!transform) {
                        let minY = Infinity;
                        data.Parts.forEach(p => {
                            const pos = parseVec3(p.Position);
                            const scale = parseVec3(p.Scale, { x: 1, y: 1, z: 1 });
                            let bottom = pos.y - (0.5 * scale.y);
                            if ((p.Shape || "").toLowerCase() === "capsule") bottom = pos.y - (1.0 * scale.y);
                            if (bottom < minY) minY = bottom;
                        });

                        if (minY !== Infinity && minY < 0) {
                            container.position.y += Math.abs(minY) * globalScaleMultiplier;
                        }
                    }

                    // Setup for interaction/drag
                    window.MCTwinGizmos.setupDrag(container);
                }
            } catch (e) { log("ERR: spawnRecipe failed: " + e.message); }
        },

        getSceneData: function () {
            const items = [];
            scene.transformNodes.forEach(n => {
                if (n.id && n.id.indexOf("recipe_") !== -1) {
                    items.push({
                        RecipeName: n.metadata ? n.metadata.recipeName : "Unknown",
                        ArtType: n.metadata ? (n.metadata.artType || "Procedural") : "Procedural",
                        Position: [n.position.x, n.position.y, n.position.z],
                        Rotation: [BABYLON.Tools.ToDegrees(n.rotation.x), BABYLON.Tools.ToDegrees(n.rotation.y), BABYLON.Tools.ToDegrees(n.rotation.z)],
                        Scale: [n.scaling.x, n.scaling.y, n.scaling.z],
                        Tags: n.metadata ? (n.metadata.tags || {}) : {}
                    });
                }
            });
            return JSON.stringify({ Name: "Exported Scene", Items: items });
        },

        loadScene: function (data) {
            log("Loading Scene: " + (data ? data.Name : "null"));
            this.clearAll();
            if (!data || !data.Items) return;
            // We can't actually SPAWN them here because we need the Recipe JSON from C#.
            // So we'll have to rely on C# iterating the data and calling spawnRecipe for each.
            // THIS function will just be a placeholder or used if C# sends the whole object.
        },

        toggleGrid: function (enabled) {
            if (gridMesh) gridMesh.setEnabled(enabled);
        },

        toggleAnimation: function (enabled) {
            isGlobalAnimating = enabled;
            log("Animation Toggled: " + (enabled ? "ON" : "OFF"));
            if (!enabled) {
                // Reset all to idle/neutral
                scene.transformNodes.forEach(node => {
                    if (node.metadata && node.metadata.artType === "Voxel") {
                        animateVoxelNodes(node, false, 0);
                    }
                });
            }
        },

        toggleDebug: function (enabled) {
            debugConsole.style.display = enabled ? "block" : "none";
        },

        updateWorld: function (config) {
            if (config.skyColor) {
                const c = BABYLON.Color3.FromHexString(config.skyColor);
                scene.clearColor = new BABYLON.Color4(c.r, c.g, c.b, 1.0);
            }
            if (config.groundColor) {
                groundMat.diffuseColor = BABYLON.Color3.FromHexString(config.groundColor);
            }
            if (config.lightIntensity !== undefined) {
                sun.intensity = config.lightIntensity;
                hemi.intensity = config.lightIntensity * 0.6;
            }
            if (config.groundVisible !== undefined) {
                ground.setEnabled(config.groundVisible);
            }
            if (config.groundSize !== undefined) {
                const s = config.groundSize / 500.0;
                ground.scaling = new BABYLON.Vector3(s, 1, s);
            }
            if (config.floorTheme) {
                const themes = {
                    'Checker': { url: "https://assets.babylonjs.com/textures/checkerboard_base.png", scale: 50 },
                    'Concrete': { url: "https://assets.babylonjs.com/textures/concrete.jpg", scale: 20 },
                    'Wood': { url: "https://assets.babylonjs.com/textures/wood.jpg", scale: 10 },
                    'Desert': { url: "https://assets.babylonjs.com/textures/sand.jpg", scale: 20 },
                    'Grass': { url: "https://assets.babylonjs.com/textures/grass.png", scale: 50 },
                    'Space': { url: "https://assets.babylonjs.com/textures/reflectivity.png", scale: 10 }
                };
                const theme = themes[config.floorTheme];
                if (theme) {
                    const tex = new BABYLON.Texture(theme.url, scene);
                    tex.uScale = theme.scale;
                    tex.vScale = theme.scale;
                    groundMat.diffuseTexture = tex;
                    if (config.floorTheme === 'Space') groundMat.diffuseColor = new BABYLON.Color3(0.1, 0.1, 0.2);
                }
            }
        },

        updateNodeTags: function (id, tags) {
            const node = scene.transformNodes.find(n => n.id === id);
            if (node && node.metadata) {
                node.metadata.tags = tags;
                log("Updated Tags for " + id);
            }
        },

        runTimeline: function (timeline, registry = null) {
            const reg = registry || this.propRegistry;
            timeline.forEach(event => {
                const target = reg[event.TargetId];
                if (!target) return;
                const frameRate = 30;
                const duration = (event.Duration || 1) * frameRate;
                if (event.Action === "Move") {
                    const endPos = parseVec3(event.Value);
                    BABYLON.Animation.CreateAndStartAnimation("anim_mv_" + event.TargetId, target, "position", frameRate, duration, target.position, endPos, 0);
                } else if (event.Action === "Rotate") {
                    const rot = parseVec3(event.Value);
                    const endRot = new BABYLON.Vector3(BABYLON.Tools.ToRadians(rot.x), BABYLON.Tools.ToRadians(rot.y), BABYLON.Tools.ToRadians(rot.z));
                    BABYLON.Animation.CreateAndStartAnimation("anim_rot_" + event.TargetId, target, "rotation", frameRate, duration, target.rotation, endRot, 0);
                } else if (event.Action === "Scale") {
                    const endScale = parseVec3(event.Value);
                    BABYLON.Animation.CreateAndStartAnimation("anim_scale_" + event.TargetId, target, "scaling", frameRate, duration, target.scaling, endScale, 0);
                } else if (event.Action === "Color" && target.material) {
                    const endCol = parseVec3(event.Value); // [r,g,b] 0-1 range
                    BABYLON.Animation.CreateAndStartAnimation("anim_col_" + event.TargetId, target.material, "albedoColor", frameRate, duration, target.material.albedoColor, new BABYLON.Color3(endCol.x, endCol.y, endCol.z), 0);
                    if (target.material.emissiveColor) BABYLON.Animation.CreateAndStartAnimation("anim_emi_" + event.TargetId, target.material, "emissiveColor", frameRate, duration, target.material.emissiveColor, new BABYLON.Color3(endCol.x, endCol.y, endCol.z), 0);
                }
            });
        },

        renderModel: function (data) {
            this.clear();
            this.spawnVoxel(data, "Player", true);
        },

        spawnVoxel: function (data, recipeName = "VoxelNPC", isPlayer = false, transform = null) {
            try {
                log("Spawning Voxel: " + recipeName + (isPlayer ? " (Player)" : " (NPC)"));
                if (!data || !data.Parts) { log("ERR: No Voxel Parts data"); return; }

                const id = isPlayer ? "root" : "recipe_" + Date.now();
                const rootNode = new BABYLON.TransformNode(id, scene);
                rootNode.metadata = { recipeName: recipeName, artType: "Voxel" };

                if (isPlayer) {
                    playerRoot = rootNode;
                } else if (transform) {
                    const pos = transform.Position || transform.position;
                    const rot = transform.Rotation || transform.rotation;
                    const scl = transform.Scale || transform.scale;

                    if (pos) rootNode.position = new BABYLON.Vector3(pos[0], pos[1], pos[2]);
                    if (rot) rootNode.rotation = new BABYLON.Vector3(BABYLON.Tools.ToRadians(rot[0]), BABYLON.Tools.ToRadians(rot[1]), BABYLON.Tools.ToRadians(rot[2]));
                    if (scl) rootNode.scaling = new BABYLON.Vector3(scl[0], scl[1], scl[2]);
                } else {
                    if (playerRoot) {
                        const forward = new BABYLON.Vector3(Math.sin(playerRoot.rotation.y), 0, Math.cos(playerRoot.rotation.y));
                        rootNode.position = playerRoot.position.add(forward.scale(2));
                        rootNode.position.y = 0;
                    }
                }

                const parts = data.Parts;
                const skinBase64 = data.Skin;

                let mat = null;
                if (skinBase64) {
                    const tex = new BABYLON.Texture(skinBase64, scene, false, true, BABYLON.Texture.NEAREST_SAMPLINGMODE);
                    tex.hasAlpha = true;
                    mat = new BABYLON.StandardMaterial("skinMat_" + id, scene);
                    mat.diffuseTexture = tex;
                    mat.specularColor = new BABYLON.Color3(0, 0, 0);
                    mat.backFaceCulling = true;
                    mat.useAlphaFromDiffuseTexture = true;
                }

                parts.forEach(p => {
                    const w = p.Dimensions[0];
                    const h = p.Dimensions[1];
                    const d = p.Dimensions[2];
                    const tw = (p.TextureDimensions && p.TextureDimensions[0]) ? p.TextureDimensions[0] : w;
                    const th = (p.TextureDimensions && p.TextureDimensions[1]) ? p.TextureDimensions[1] : h;
                    const td = (p.TextureDimensions && p.TextureDimensions[2]) ? p.TextureDimensions[2] : d;
                    const u0 = p.TextureOffset[0];
                    const v0 = p.TextureOffset[1];
                    const pixel = 1.0 / 64.0;
                    const getUV = (xPixel, yPixel, wPixel, hPixel) => {
                        const uMin = xPixel * pixel;
                        const uMax = (xPixel + wPixel) * pixel;
                        const vMax = 1.0 - (yPixel * pixel);
                        const vMin = 1.0 - ((yPixel + hPixel) * pixel);
                        return new BABYLON.Vector4(uMin, vMin, uMax, vMax);
                    };
                    const faceUV = [];
                    faceUV[0] = getUV(u0 + td, v0 + td, tw, th);
                    faceUV[1] = getUV(u0 + td + tw + td, v0 + td, tw, th);
                    faceUV[2] = getUV(u0, v0 + td, td, th);
                    faceUV[3] = getUV(u0 + td + tw, v0 + td, td, th);
                    faceUV[4] = getUV(u0 + td, v0, tw, td);
                    faceUV[5] = getUV(u0 + td + tw, v0, tw, td);

                    const mesh = BABYLON.MeshBuilder.CreateBox(p.Name + "_" + id, {
                        width: w / 16, height: h / 16, depth: d / 16, faceUV: faceUV, wrap: true
                    }, scene);

                    mesh.parent = rootNode;
                    mesh.position.set(p.Offset[0] / 16, p.Offset[1] / 16, p.Offset[2] / 16);

                    if (p.Pivot && (p.Pivot[0] !== 0 || p.Pivot[1] !== 0 || p.Pivot[2] !== 0)) {
                        const localP = new BABYLON.Vector3(
                            (p.Pivot[0] - p.Offset[0]) / 16,
                            (p.Pivot[1] - p.Offset[1]) / 16,
                            (p.Pivot[2] - p.Offset[2]) / 16
                        );
                        mesh.setPivotPoint(localP);
                    }

                    if (p.Rotation) {
                        mesh.rotation.x = BABYLON.Tools.ToRadians(p.Rotation[0]);
                        mesh.rotation.y = BABYLON.Tools.ToRadians(p.Rotation[1]);
                        mesh.rotation.z = BABYLON.Tools.ToRadians(p.Rotation[2]);
                    }

                    if (mat) mesh.material = mat;
                    else {
                        const dumbMat = new BABYLON.StandardMaterial("dumb_" + id, scene);
                        dumbMat.diffuseColor = p.HexColor ? BABYLON.Color3.FromHexString(p.HexColor) : new BABYLON.Color3(1, 1, 1);
                        mesh.material = dumbMat;
                    }

                    shadowGenerator.addShadowCaster(mesh);
                    if (isPlayer) meshMap.set(p.Name, mesh);
                });

                // Setup for interaction/drag
                window.MCTwinGizmos.setupDrag(rootNode);
            } catch (e) { log("ERR: spawnVoxel failed: " + e.message); }
        }
    };

    var pipeline = new BABYLON.DefaultRenderingPipeline("pp", true, scene, [camera]);
    pipeline.fxaaEnabled = true;
    pipeline.bloomEnabled = true;
    pipeline.bloomThreshold = 0.6;
    pipeline.bloomWeight = 0.4;
    pipeline.bloomKernel = 64;
    pipeline.bloomScale = 0.5;

    engine.runRenderLoop(() => scene.render());
    return scene;
};
const scene = createScene();
window.addEventListener("resize", () => engine.resize());
