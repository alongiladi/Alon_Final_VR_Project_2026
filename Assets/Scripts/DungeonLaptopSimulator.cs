using UnityEngine;
using UnityEngine.InputSystem; //new input System package namespaces

///  laptop simulator for testing the VR Escape Dungeon game
/// without needing headset. Compatible with New Input System.
/// 
/// CONTROLS:
/// - Movement: [W, A, S, D] keys ( movement)
/// - Look Around:
///     1. Arrow Keys: [Left / Right Arrow] to turn, [Up / Down Arrow] to look up/down
///     2. Mouse: Hold [Right Mouse Button] + Move Mouse
/// - Grab / Drop: Press [Left Click], [F], [G], or [E] when looking at items
/// - failsafe:  boundaries and safety so playernot  falls off the map
public class DungeonLaptopSimulator : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Tooltip("Enable keyboard/mouse simulation inside the Unity Editor even if no headset is connected.")]
    public bool enableInEditor = true;

    [Tooltip("Movement speed when walking with WASD.")]
    public float moveSpeed = 3.5f;

    [Header("Look Controls (Mouse & Arrow Keys)")]
    [Tooltip("Smooth mouse sensitivity when looking around (lower = smoother & less twitchy).")]
    public float mouseLookSensitivity = 0.04f;

    [Tooltip("Turn speed in degrees per second when looking with Arrow Keys.")]
    public float arrowKeyTurnSpeed = 60f;

    [Tooltip("The maximum distance from which you can grab an item.")]
    public float grabRange = 5.0f;

    [Header("Look Clamping (Look Restrictions)")]
    [Tooltip("How far down you can look. Prevents looking 'under' the player character.")]
    public float maxLookDownAngle = 75f;
    [Tooltip("How far up you can look.")]
    public float maxLookUpAngle = 75f;

    [Header("Hold Target Offsets (Dual-Wield Torch + Key)")]
    [Tooltip("Where the held torch hovers (Left Hand).")]
    public Vector3 leftHoldOffset = new Vector3(-0.35f, -0.25f, 0.65f);
    [Tooltip("Where the held key hovers (Right Hand).")]
    public Vector3 rightHoldOffset = new Vector3(0.35f, -0.22f, 0.65f);

    [Header("Jump Settings")]
    [Tooltip("Upward velocity applied when pressing Space to jump.")]
    public float jumpForce = 4.5f;
    [Tooltip("Downward gravity acceleration.")]
    public float gravity = 15f;

    private Transform xrOriginTransform;
    private Camera playerCamera;
    private CharacterController characterController;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float verticalVelocity = 0f;

    // boundary limits calculated dynamically
    private bool useBoundaryLimits = false;
    private float minBoundX;
    private float maxBoundX;
    private float minBoundZ;
    private float maxBoundZ;
    private Vector3 initialSpawnPosition = Vector3.zero;

    // Dual-Wield Grab State (Left slot = Torch, Right slot = Key / other)
    private GameObject heldTorch = null;
    private Transform torchOriginalParent = null;
    private Rigidbody torchRigidbody = null;

    private GameObject heldKey = null;
    private Transform keyOriginalParent = null;
    private Rigidbody keyRigidbody = null;

    private void Start()
    {
        // 1. Detect if VR headset  active 
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Debug.Log("[DungeonLaptopSimulator] VR Device is active. Laptop simulator disabled.");
            this.enabled = false;
            return;
        }

        if (!enableInEditor && Application.isEditor)
        {
            Debug.Log("[DungeonLaptopSimulator] Simulator disabled in Editor per configuration.");
            this.enabled = false;
            return;
        }

        Debug.Log("[DungeonLaptopSimulator] VR Headset not detected. Starting Laptop Keyboard/Mouse Simulation!");
        
        // Find XR Origin and Main Camera
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            xrOriginTransform = playerCamera.transform.parent;
            if (xrOriginTransform != null && xrOriginTransform.name.Contains("Camera Offset"))
            {
                if (xrOriginTransform.parent != null)
                {
                    xrOriginTransform = xrOriginTransform.parent;
                }
            }

            if (xrOriginTransform != null)
            {
                characterController = xrOriginTransform.GetComponent<CharacterController>();
                initialSpawnPosition = xrOriginTransform.position;
            }

            // Initialize camera rotation angles to current orientation
            rotationY = xrOriginTransform != null ? xrOriginTransform.eulerAngles.y : playerCamera.transform.eulerAngles.y;
            rotationX = 0f;
        }
        else
        {
            Debug.LogError("[DungeonLaptopSimulator] Could not find Main Camera in scene. Simulator will not function.");
            this.enabled = false;
            return;
        }

        UpdateMazeBoundaries();
    }

    
    /// Resets simulator look yaw to match a new teleport rotation.
    
    public void SyncRotation(float targetYaw)
    {
        rotationY = targetYaw;
        rotationX = 0f;
        if (xrOriginTransform != null)
        {
            xrOriginTransform.rotation = Quaternion.Euler(0, rotationY, 0);
        }
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.identity;
        }
    }

    
    // Calculates  bounding limits based on generated maze grid.
    
    private void UpdateMazeBoundaries()
    {
        var mazeGen = GameObject.FindAnyObjectByType<MazeGenerator>();
        if (mazeGen != null)
        {
            float cellSize = mazeGen.cellSize;
            int width = mazeGen.width;
            int depth = mazeGen.depth;

            // Boundaries encompass the full floor tiles with safe padding
            minBoundX = -cellSize / 2f + 0.4f;
            maxBoundX = (width - 1) * cellSize + (cellSize / 2f) - 0.4f;
            minBoundZ = -cellSize / 2f + 0.4f;
            maxBoundZ = (depth - 1) * cellSize + (cellSize / 2f) - 0.4f;
            useBoundaryLimits = true;
        }
    }

    private void Update()
    {
        if (playerCamera == null || xrOriginTransform == null) return;

        // Ensure boundary limits are up to date
        if (!useBoundaryLimits)
        {
            UpdateMazeBoundaries();
        }

        HandleRotation();
        HandleMovement();
        HandleInteraction();
        HandleFractalTreeControls();
        ApplyBoundaryAndFallProtection();
    }

    /// <summary>
    /// Keys [1, 2, 3, 4] to interactively modify the procedural fractal tree in runtime.
    /// </summary>
    private void HandleFractalTreeControls()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var tree = GameObject.FindAnyObjectByType<InteractiveFractalTree>();
        if (tree == null) return;

        if (keyboard.digit1Key != null && keyboard.digit1Key.wasPressedThisFrame)
        {
            tree.IncreaseDepth();
            Debug.Log($"[DungeonLaptopSimulator] Fractal Tree Depth increased to {tree.recursionDepth}");
        }
        else if (keyboard.digit2Key != null && keyboard.digit2Key.wasPressedThisFrame)
        {
            tree.DecreaseDepth();
            Debug.Log($"[DungeonLaptopSimulator] Fractal Tree Depth decreased to {tree.recursionDepth}");
        }
        else if (keyboard.digit3Key != null && keyboard.digit3Key.wasPressedThisFrame)
        {
            tree.SetBranchAngle(tree.branchAngle + 5f);
            Debug.Log($"[DungeonLaptopSimulator] Fractal Tree Branch Angle set to {tree.branchAngle}");
        }
        else if (keyboard.digit4Key != null && keyboard.digit4Key.wasPressedThisFrame)
        {
            tree.SetBranchAngle(tree.branchAngle - 5f);
            Debug.Log($"[DungeonLaptopSimulator] Fractal Tree Branch Angle set to {tree.branchAngle}");
        }
    }

    /// <summary>
    /// Rotates the camera using:
    /// 1. Smooth Mouse Drag (Holding Right Mouse Button with reduced sensitivity)
    /// 2. Arrow Keys (Left/Right to turn, Up/Down to pitch)
    /// </summary>
    private void HandleRotation()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        float deltaYaw = 0f;
        float deltaPitch = 0f;

        // 1. Mouse Look (when holding Right Mouse Button)
        if (mouse != null && mouse.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 mouseDelta = mouse.delta.ReadValue() * mouseLookSensitivity;
            deltaYaw += mouseDelta.x;
            deltaPitch -= mouseDelta.y;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 2. Arrow Keys Look (simultaneous with WASD movement)
        if (keyboard != null)
        {
            float arrowHorizontal = 0f;
            float arrowVertical = 0f;

            if (keyboard.leftArrowKey.isPressed) arrowHorizontal -= 1f;
            if (keyboard.rightArrowKey.isPressed) arrowHorizontal += 1f;
            if (keyboard.upArrowKey.isPressed) arrowVertical -= 1f; // look up
            if (keyboard.downArrowKey.isPressed) arrowVertical += 1f; // look down

            if (arrowHorizontal != 0f || arrowVertical != 0f)
            {
                deltaYaw += arrowHorizontal * arrowKeyTurnSpeed * Time.deltaTime;
                deltaPitch += arrowVertical * arrowKeyTurnSpeed * Time.deltaTime;
            }
        }

        // Apply rotation changes if any occurred
        if (deltaYaw != 0f || deltaPitch != 0f)
        {
            rotationY += deltaYaw;
            rotationX += deltaPitch;

            // Clamp vertical pitch so you can look comfortably at the floor without looking inside your body or inverting
            rotationX = Mathf.Clamp(rotationX, -maxLookUpAngle, maxLookDownAngle);

            xrOriginTransform.rotation = Quaternion.Euler(0, rotationY, 0);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
    }

    /// <summary>
    /// Translates the player rig in 3D space using ONLY W, A, S, D keys and Space to jump.
    /// </summary>
    private void HandleMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float moveH = 0f;
        float moveV = 0f;

        // Strictly WASD for movement (Arrow keys are reserved for looking around!)
        if (keyboard.wKey.isPressed) moveV += 1f;
        if (keyboard.sKey.isPressed) moveV -= 1f;
        if (keyboard.aKey.isPressed) moveH -= 1f;
        if (keyboard.dKey.isPressed) moveH += 1f;

        // Jump handling via Space key ONLY
        bool isGrounded = characterController != null ? characterController.isGrounded : (xrOriginTransform.position.y <= 0.05f);

        if (isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -1f; // Slight downward pressure to keep grounded
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = jumpForce;
                Debug.Log("[DungeonLaptopSimulator] Jump triggered with Space key!");
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        // Keep horizontal movement pure
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 horizontalMove = (forward * moveV + right * moveH).normalized * moveSpeed;
        Vector3 finalVelocity = horizontalMove + Vector3.up * verticalVelocity;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(finalVelocity * Time.deltaTime);
        }
        else
        {
            xrOriginTransform.position += finalVelocity * Time.deltaTime;
            if (xrOriginTransform.position.y < 0f)
            {
                Vector3 p = xrOriginTransform.position;
                p.y = 0f;
                xrOriginTransform.position = p;
                verticalVelocity = 0f;
            }
        }
    }

    /// <summary>
    /// Absolute protection: clamps X, Z within maze boundaries, and resets Y if falling into the void.
    /// </summary>
    private void ApplyBoundaryAndFallProtection()
    {
        Vector3 pos = xrOriginTransform.position;
        bool positionChanged = false;

        // 1. Fall Protection: If player drops below the floor (Y < -0.1f), clamp back to floor level Y = 0
        if (pos.y < -0.1f)
        {
            pos.y = 0f;
            positionChanged = true;
            Debug.Log("[DungeonLaptopSimulator] Fall protection prevented player from falling into the void!");
        }
        else if (pos.y > 0.05f && characterController == null)
        {
            // If flying up due to any simulator key, pull back down
            pos.y = Mathf.MoveTowards(pos.y, 0f, 6f * Time.deltaTime);
            positionChanged = true;
        }

        // 2. Horizontal Boundary Protection
        if (useBoundaryLimits)
        {
            float clampedX = Mathf.Clamp(pos.x, minBoundX, maxBoundX);
            float clampedZ = Mathf.Clamp(pos.z, minBoundZ, maxBoundZ);

            if (pos.x != clampedX || pos.z != clampedZ)
            {
                pos.x = clampedX;
                pos.z = clampedZ;
                positionChanged = true;
            }
        }

        // Re-apply clamped position to character controller without physics glitches
        if (positionChanged)
        {
            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
                xrOriginTransform.position = pos;
                characterController.enabled = true;
            }
            else
            {
                xrOriginTransform.position = pos;
            }
        }
    }

    /// <summary>
    /// Grabs and drops physical items using Left-Click, F, G, or E.
    /// Supports holding the Torch in the Left Hand and the Key in the Right Hand simultaneously!
    /// </summary>
    private void HandleInteraction()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        bool interactPressed = (keyboard.fKey != null && keyboard.fKey.wasPressedThisFrame)
                            || (keyboard.eKey != null && keyboard.eKey.wasPressedThisFrame)
                            || (mouse.leftButton != null && mouse.leftButton.wasPressedThisFrame);

        bool dropPressed = (keyboard.gKey != null && keyboard.gKey.wasPressedThisFrame)
                        || (keyboard.qKey != null && keyboard.qKey.wasPressedThisFrame);

        if (interactPressed)
        {
            TryGrabObject();
        }

        if (dropPressed)
        {
            DropAnyObject();
        }
    }

    /// <summary>
    /// Casts a SphereCast from the camera center to comfortably grab items.
    /// Distinguishes between Torches (Left Hand) and Keys (Right Hand).
    /// </summary>
    private void TryGrabObject()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.SphereCast(ray, 0.45f, out hit, grabRange) || Physics.Raycast(ray, out hit, grabRange))
        {
            GameObject hitObj = hit.collider.gameObject;
            var grabComponent = hitObj.GetComponent("XRGrabInteractable");
            
            if (grabComponent == null && hitObj.transform.parent != null)
            {
                hitObj = hitObj.transform.parent.gameObject;
                grabComponent = hitObj.GetComponent("XRGrabInteractable");
            }

            if (grabComponent != null)
            {
                bool isTorch = hitObj.name.ToLower().Contains("torch");
                bool isKey = hitObj.name.ToLower().Contains("key") || hitObj.CompareTag("Key");

                if (isTorch)
                {
                    GrabTorch(hitObj);
                }
                else if (isKey)
                {
                    GrabKey(hitObj);
                }
                else
                {
                    // Generic grab into right hand
                    GrabKey(hitObj);
                }
            }
        }
    }

    private void GrabTorch(GameObject torchObj)
    {
        if (heldTorch != null && heldTorch != torchObj)
        {
            DropTorch();
        }

        heldTorch = torchObj;
        torchOriginalParent = heldTorch.transform.parent;

        torchRigidbody = heldTorch.GetComponent<Rigidbody>();
        if (torchRigidbody != null)
        {
            torchRigidbody.isKinematic = true;
            torchRigidbody.useGravity = false;
            torchRigidbody.linearVelocity = Vector3.zero;
            torchRigidbody.angularVelocity = Vector3.zero;
        }

        heldTorch.transform.SetParent(playerCamera.transform);
        heldTorch.transform.localPosition = leftHoldOffset;
        heldTorch.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

        Debug.Log($"[DungeonLaptopSimulator] Grabbed Torch into Left Hand: {heldTorch.name}");
    }

    private void GrabKey(GameObject keyObj)
    {
        if (heldKey != null && heldKey != keyObj)
        {
            DropKey();
        }

        heldKey = keyObj;
        keyOriginalParent = heldKey.transform.parent;

        keyRigidbody = heldKey.GetComponent<Rigidbody>();
        if (keyRigidbody != null)
        {
            keyRigidbody.linearVelocity = Vector3.zero;
            keyRigidbody.angularVelocity = Vector3.zero;
            keyRigidbody.isKinematic = true;
            keyRigidbody.useGravity = false;
        }

        heldKey.transform.SetParent(playerCamera.transform);
        heldKey.transform.localPosition = rightHoldOffset;
        heldKey.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

        Debug.Log($"[DungeonLaptopSimulator] Grabbed Key into Right Hand: {heldKey.name}");
    }

    private void DropTorch()
    {
        if (heldTorch == null) return;

        heldTorch.transform.SetParent(torchOriginalParent);
        if (torchRigidbody != null)
        {
            torchRigidbody.isKinematic = false;
            torchRigidbody.useGravity = true;
            torchRigidbody.linearVelocity = Vector3.zero;
            torchRigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[DungeonLaptopSimulator] Dropped Torch: {heldTorch.name}");
        heldTorch = null;
        torchRigidbody = null;
    }

    private void DropKey()
    {
        if (heldKey == null) return;

        heldKey.transform.SetParent(keyOriginalParent);
        if (keyRigidbody != null)
        {
            keyRigidbody.isKinematic = false;
            keyRigidbody.useGravity = true;
            keyRigidbody.linearVelocity = Vector3.zero;
            keyRigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[DungeonLaptopSimulator] Dropped Key: {heldKey.name}");
        heldKey = null;
        keyRigidbody = null;
    }

    private void DropAnyObject()
    {
        if (heldKey != null)
        {
            DropKey();
        }
        else if (heldTorch != null)
        {
            DropTorch();
        }
    }

    private void OnGUI()
    {
        if (!this.enabled) return;

        // Custom HUD panel
        Rect rect = new Rect(20, 20, 390, 200);
        GUI.Box(rect, "VR Dungeon Laptop Controls");
        
        GUI.Label(new Rect(30, 45, 370, 22), "• Walk: [W, A, S, D] keys | Jump: [Space]");
        GUI.Label(new Rect(30, 68, 370, 22), "• Look: [Arrow Keys] OR Hold [Right Click] + Mouse");
        GUI.Label(new Rect(30, 91, 370, 22), "• Grab: [Left Click], [F], or [E] (Dual-wield Torch + Key!)");
        GUI.Label(new Rect(30, 114, 370, 22), "• Drop: [G] or [Q]");
        
        string torchStatus = heldTorch != null ? "Torch (Left Hand)" : "Empty";
        string keyStatus = heldKey != null ? "Golden Key (Right Hand)" : "Empty";
        GUI.Label(new Rect(30, 140, 370, 22), $"• Inventory: L: [{torchStatus}] | R: [{keyStatus}]");
        GUI.Label(new Rect(30, 163, 370, 22), "• Goal: Carry Key to Room 3 Exit Lock Box!");

        // Center crosshair
        float xMin = (Screen.width / 2f) - 6f;
        float yMin = (Screen.height / 2f) - 6f;
        GUI.Box(new Rect(xMin, yMin, 12f, 12f), "+");
    }
}




