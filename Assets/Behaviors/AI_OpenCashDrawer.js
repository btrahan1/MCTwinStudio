window.MCTwinBehaviors['AI_OpenCashDrawer'] = {
  onInteract: (node, args) => {
    if (!node.metadata) node.metadata = {};
    
    // Find the drawer mesh within the cash register parts
    const drawer = node.getChildMeshes().find(m => m.name.includes('drawer_front') || m.id === 'drawer_front');
    
    if (drawer) {
      const isOpen = !!node.metadata.isDrawerOpen;
      const closedZ = 0.28; // Original position from JSON
      const openZ = 0.55;   // Extended position
      const targetZ = isOpen ? closedZ : openZ;

      // Animation configuration
      const frameRate = 30;
      const moveAnim = new BABYLON.Animation(
        'drawerSlide',
        'position.z',
        frameRate,
        BABYLON.Animation.ANIMATIONTYPE_FLOAT,
        BABYLON.Animation.ANIMATIONLOOPMODE_CONSTANT
      );

      const keys = [
        { frame: 0, value: drawer.position.z },
        { frame: 10, value: targetZ }
      ];
      moveAnim.setKeys(keys);

      // Easing for a smooth mechanical feel
      const easingFunction = new BABYLON.BackEase(0.5);
      easingFunction.setEasingMode(BABYLON.EasingFunction.EASINGMODE_EASEOUT);
      moveAnim.setEasingFunction(easingFunction);

      drawer.animations = [moveAnim];
      node.getScene().beginAnimation(drawer, 0, 10, false);

      // Toggle state
      node.metadata.isDrawerOpen = !isOpen;
    }
  },
  onTick: (node, args, time) => {
    // No continuous motion required for this behavior
  }
};