window.SalesTwin = {
    scene: null,
    camera: null,
    renderer: null,
    robot: null,
    taskQueue: [],
    isBusy: false,

    // Config
    rackLocations: {
        'WIDGET-A': { x: -4, z: -2 },
        'WIDGET-B': { x: -4, z: 2 },
        'GADGET-X': { x: 4, z: -2 },
        'GADGET-Y': { x: 4, z: 2 },
    },
    dockLocation: { x: 0, z: 8 },

    init: function (containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x202030); // Dark Blueish

        // Camera
        this.camera = new THREE.PerspectiveCamera(60, container.clientWidth / container.clientHeight, 0.1, 1000);
        this.camera.position.set(0, 15, 15);
        this.camera.lookAt(0, 0, 0);

        // Renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setSize(container.clientWidth, container.clientHeight);
        container.appendChild(this.renderer.domElement);

        // Lighting
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.7);
        this.scene.add(ambientLight);
        this.scene.add(new THREE.GridHelper(20, 20));

        // Create Environment
        this.createRacks();
        this.createRobot();
        this.createDock();

        // Loop
        this.animate();
    },

    createRacks: function () {
        const geo = new THREE.BoxGeometry(2, 4, 1);
        const mat = new THREE.MeshLambertMaterial({ color: 0x888888 });

        for (const [sku, loc] of Object.entries(this.rackLocations)) {
            const rack = new THREE.Mesh(geo, mat);
            rack.position.set(loc.x, 2, loc.z);
            this.scene.add(rack);

            // Label (Text is hard in pure Three.js without fonts, using colored box on top to ID)
            const label = new THREE.Mesh(new THREE.BoxGeometry(1, 0.5, 1.2), new THREE.MeshBasicMaterial({ color: this.getColor(sku) }));
            label.position.set(loc.x, 4.5, loc.z);
            this.scene.add(label);
        }
    },

    createRobot: function () {
        // Simple Robot
        const group = new THREE.Group();
        const body = new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1), new THREE.MeshLambertMaterial({ color: 0xFFD700 })); // Gold
        body.position.y = 0.5;
        group.add(body);

        this.robot = group;
        this.scene.add(this.robot);
    },

    createDock: function () {
        const dock = new THREE.Mesh(new THREE.BoxGeometry(6, 0.2, 4), new THREE.MeshLambertMaterial({ color: 0x333333 }));
        dock.position.set(0, 0.1, 8);
        this.scene.add(dock);
    },

    animate: function () {
        requestAnimationFrame(() => this.animate());

        if (this.robot && !this.isBusy && this.taskQueue.length > 0) {
            const task = this.taskQueue.shift();
            this.executeTask(task);
        }

        if (this.renderer && this.scene && this.camera) {
            this.renderer.render(this.scene, this.camera);
        }

        // TWEEN update equivalent (simple interpolation logic could go here)
    },

    enqueueTask: function (sku, quantity) {
        console.log(`Job Received: Fetch ${quantity} of ${sku}`);
        this.taskQueue.push({ sku, quantity });
    },

    executeTask: function (task) {
        this.isBusy = true;
        const target = this.rackLocations[task.sku] || { x: 0, z: 0 };

        console.log("Starting task...", task);

        // Simple animation sequence simulation using async/await and timeouts
        // In a real engine, use TWEEN.js or a delta-time mover.

        // 1. Go to Rack
        this.tweenTo(target, 1000, () => {
            // 2. Wait (Pick)
            this.blinkRobot();
            setTimeout(() => {
                // 3. Go to Dock
                this.tweenTo(this.dockLocation, 1000, () => {
                    // 4. Drop
                    console.log("Delivered!");
                    this.isBusy = false;
                });
            }, 500);
        });
    },

    tweenTo: function (targetPos, duration, onComplete) {
        const startPos = { x: this.robot.position.x, z: this.robot.position.z };
        const startTime = Date.now();

        const update = () => {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);

            this.robot.position.x = startPos.x + (targetPos.x - startPos.x) * progress;
            this.robot.position.z = startPos.z + (targetPos.z - startPos.z) * progress;

            if (progress < 1) {
                requestAnimationFrame(update);
            } else {
                if (onComplete) onComplete();
            }
        };
        update();
    },

    blinkRobot: function () {
        // Flash visual
        this.robot.children[0].material.color.setHex(0xFF0000);
        setTimeout(() => this.robot.children[0].material.color.setHex(0xFFD700), 200);
    },

    getColor: function (sku) {
        let hash = 0;
        for (let i = 0; i < sku.length; i++) {
            hash = sku.charCodeAt(i) + ((hash << 5) - hash);
        }
        const c = (hash & 0x00FFFFFF).toString(16).toUpperCase();
        return '#' + '00000'.substring(0, 6 - c.length) + c;
    }
};
