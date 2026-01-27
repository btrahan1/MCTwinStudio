window.Warehouse = {
    scene: null,
    camera: null,
    renderer: null,
    cubes: {}, // Map "Rack-Shelf-Bin" -> Mesh

    init: function (containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // 1. Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x303030);

        // 2. Camera
        this.camera = new THREE.PerspectiveCamera(75, container.clientWidth / container.clientHeight, 0.1, 1000);
        this.camera.position.set(5, 5, 10);
        this.camera.lookAt(0, 0, 0);

        // 3. Renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setSize(container.clientWidth, container.clientHeight);
        container.appendChild(this.renderer.domElement);

        // 4. Lights
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        this.scene.add(ambientLight);
        const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
        dirLight.position.set(10, 20, 10);
        this.scene.add(dirLight);

        // 5. Grid Helper
        const gridHelper = new THREE.GridHelper(20, 20);
        this.scene.add(gridHelper);

        // Start Loop
        this.animate();
    },

    animate: function () {
        requestAnimationFrame(() => this.animate());
        if (this.renderer && this.scene && this.camera) {
            this.renderer.render(this.scene, this.camera);
        }
    },

    updateInventory: function (items) {
        if (!this.scene) return;

        // Items is an array of { rackIndex, shelfLevel, binIndex, productSku, quantity }

        // Simple visualization:
        // Rack -> X
        // Shelf -> Y
        // Bin -> Z

        items.forEach(item => {
            const key = `${item.rackIndex}-${item.shelfLevel}-${item.binIndex}`;

            // Layout Logic:
            // Racks separated by 3 units on X (0, 3, 6...)
            // Shelves separated by 1.5 units on Y (0.5, 2.0, 3.5...)
            // Bins separated by 1.2 units on Z (-2.4 to +2.4)

            const x = item.rackIndex * 4;
            const y = item.shelfLevel * 1.5 + 0.5;
            const z = (item.binIndex - 2) * 1.2; // Center the 5 bins

            if (item.quantity > 0) {
                if (!this.cubes[key]) {
                    // Create Box
                    const geometry = new THREE.BoxGeometry(0.8, 0.8, 0.8);
                    const material = new THREE.MeshLambertMaterial({ color: this.getColor(item.productSku) });
                    const cube = new THREE.Mesh(geometry, material);
                    cube.position.set(x, y, z);

                    this.scene.add(cube);
                    this.cubes[key] = cube;
                }
                // Ensure visible
                this.cubes[key].visible = true;
            } else {
                // Quantity 0, hide if exists
                if (this.cubes[key]) {
                    this.cubes[key].visible = false;
                }
            }
        });
    },

    getColor: function (sku) {
        // Simple hash to color
        let hash = 0;
        for (let i = 0; i < sku.length; i++) {
            hash = sku.charCodeAt(i) + ((hash << 5) - hash);
        }
        const c = (hash & 0x00FFFFFF).toString(16).toUpperCase();
        return '#' + '00000'.substring(0, 6 - c.length) + c;
    }
};
