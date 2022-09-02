using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunPlayerScript : MonoBehaviour
{
    // Start is called before the first frame update


    /*
        /////////////
        /////////////           TODO 
        /////////////   make hand line up with gun's handle

                        Bmake a second gun, and an equip / unequip method

                            Nmake him look at target
                        
                        Calculate distance from body based on handle distance, make arm be able to fully extend

        /////////////
        /////////////           BACKBURNER  
        /////////////   determine, based on the gun's handles how far to hold the gun
    */

    // ------------------------------ player variables
    Rigidbody2D _rigidbody;

    Animator _animator;

    Transform _head;
    Transform _frontShoulder;
    Transform _frontElbow;
    Transform _frontWrist;

    float _frontArmFullLength;
    float _frontUpperArmLength;
    float _frontForearmLength;

    bool _facingRight = true;
    float _moveSpeed = 10;


    // ------------------------------ aiming rig variables

    Camera _camera;
    Transform _frontArmMinRangeIndicator; Transform _frontArmMaxRangeIndicator;
    Transform _aimOrigin;
    Transform _aimTarget;
    Transform _aimCenter;
    Transform _gun;
    Transform _frontHandle;
    Transform _backHandle;

    float _minHoldingDistance = 1; // how far away from the origin point (front shoulder) the gun must be held
    public float _maxHoldingDistance = 1.5f; // how far away from the origin point (front shoulder) the gun can be held

    bool _canShoot = true; // determines if the gun is able to be fired (disabled between shots/while reloading)




    // ------------------------------ gun variables

    GunScript _gunScript;
    GameObject _ammo;
    Animation _fireAnimation;

    //semi auto, full auto, burst
    //0-none, 1-semi, 2-full auto, 3-burst
    int _firingMode;
    int _clipSize;
    float _reloadTime; // time it takes to reload gun
    float _firePower = 150f;

    float _delayTime; // time between shots


    void Start()
    {

         _camera = GetComponentInParent<Camera>();

        _rigidbody = GetComponent<Rigidbody2D>();

        _head = GameObject.Find("Head").GetComponent<Transform>();
        _frontShoulder = GameObject.Find("FrontShoulder").GetComponent<Transform>();
        _frontElbow = GameObject.Find("FrontElbow").GetComponent<Transform>();
        _frontWrist = GameObject.Find("FrontWrist").GetComponent<Transform>();
        _animator = GameObject.Find("Hip").GetComponent<Animator>();

        _frontArmMinRangeIndicator = GameObject.Find("FrontArmMinRangeIndicator").GetComponent<Transform>();
        _frontArmMaxRangeIndicator = GameObject.Find("FrontArmMaxRangeIndicator").GetComponent<Transform>();

        _aimTarget = GameObject.Find("AimTarget").GetComponent<Transform>();
        _aimOrigin = GameObject.Find("AimOrigin").GetComponent<Transform>();
        _aimCenter = GameObject.Find("AimCenter").GetComponent<Transform>();



        //calculating variables based on player's body size
        _frontArmFullLength = (_frontShoulder.position - _frontWrist.position).magnitude;
        _frontUpperArmLength = (_frontShoulder.position - _frontElbow.position).magnitude;
        _frontForearmLength = (_frontElbow.position - _frontWrist.position).magnitude;

        //determining how far the gun is able to be held, based on the length of the player's arms
        _minHoldingDistance = (_frontShoulder.position - _frontElbow.position).magnitude;
        _maxHoldingDistance = _frontArmFullLength;

        SetupGun();
        
        //setting the range indicators for the front arm
        _frontArmMinRangeIndicator.transform.position = _frontShoulder.position;
        _frontArmMinRangeIndicator.localScale = new Vector2(_minHoldingDistance, _minHoldingDistance) * 2;
        _frontArmMaxRangeIndicator.transform.position = _frontShoulder.position;
        _frontArmMaxRangeIndicator.localScale = new Vector2(_maxHoldingDistance, _maxHoldingDistance) * 2;

        

    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Aim();
        AnimateBody();
        FlipPlayer();

        if (_firingMode == 1 && Input.GetMouseButtonDown(0))
        {
            //attempt firing gun
            Fire();
        }
        if (_firingMode == 2 && Input.GetMouseButton(0))
        {
            //attempt firing gun
            Fire();
        }
    }

    void SetupGun(){
        //called when the game starts, or when switching to a new gun
        _gunScript = transform.GetComponentInChildren<GunScript>();

        _gun =_gunScript.GetComponent<Transform>();
        _fireAnimation = _gun.GetComponent<Animation>();

        _frontHandle = _gun.Find("FrontHandle").GetComponent<Transform>();
        _backHandle = _gun.Find("BackHandle").GetComponent<Transform>();

        (_ammo ,_firingMode, _clipSize, _reloadTime, _delayTime) = _gunScript.GetVars();

        //trying to offset gun by handles
        //float gunToHandleDist = Mathf.Abs(_gun.position.y - _frontHandle.position.y);
        //_maxHoldingDistance += gunToHandleDist;

    }

    void FlipPlayer() // checks if player needs to be flipped horizontally (facing left vs right)
    {
        if (_aimTarget.position.x - _frontShoulder.position.x >= 0 && transform.localScale.x != 1)
        {
            transform.localScale = new Vector2(1, 1);
            _facingRight = true;
        }
        else if (_aimTarget.position.x - _frontShoulder.position.x <= 0 && transform.localScale.x != -1)
        {
            transform.localScale = new Vector2(-1, 1);
            _facingRight = false;
        }
    }
    void Fire() // gun shoots one shot
    {
        if (!_canShoot)
        {
            return;
        }
        Vector2 bulletDirection = _aimTarget.position - _aimOrigin.position;
        Vector2 bulletVelocity = Vector3.Normalize(bulletDirection);
        bulletVelocity *= _firePower;

        GameObject newBullet = Instantiate(_ammo, _gun.position, Quaternion.LookRotation(Vector3.forward, bulletDirection));
        newBullet.GetComponent<Rigidbody2D>().velocity = bulletVelocity;

        StartCoroutine(FireDelay(_delayTime));
        print("Done " + Time.time);
    }

    private IEnumerator FireDelay(float delay)
    {
        _fireAnimation.Play();
        _canShoot = false;
        yield return new WaitForSeconds(delay);
        _canShoot = true;
    }

    void Aim()  //function aims the gun rig, including the targets, and the actual gun
    {
        #region aiming/trig
        //TODO, make a minimum arm length range - so the player cant hold the gun directly on top of his body or too close to it
        //fix the strangle little behaviour when aiming right throught the shoulder+eyes line

        // converts the mouse position to a point in the game's worldspace
        Vector2 cameraWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //moves the aiming visual to mouse's position
        _aimTarget.position = cameraWorldPos;

        //the gun's position
        Vector2 gunPos;

        if ((new Vector2(_frontShoulder.position.x, _frontShoulder.position.y) - cameraWorldPos).magnitude <= _maxHoldingDistance)
        {
            //if the mouse position is within the player's reach, position the gun on the mouse
            //Debug.Log("I CAN REACH IT NO PROBLEM"); 

            //calculates a point a set distance away from the player's aim origin point(player's head), toward the player's aim target
            Vector2 originPos = new Vector2(_aimOrigin.position.x, _aimOrigin.position.y);
            Vector2 relativePos = new Vector2(cameraWorldPos.x - _aimOrigin.position.x, cameraWorldPos.y - _aimOrigin.position.y);
            gunPos = originPos + relativePos;

            //calculates the new restricted point to worldspace relative to the player's position
            if ((originPos - (originPos + relativePos)).magnitude > _maxHoldingDistance)
            {
                gunPos = Vector2.ClampMagnitude(relativePos, _maxHoldingDistance) + originPos;
            }
        }
        else
        {
            // if the mouse position is outside the player's reach, calculate the point the gun can be held

            // the new position with be along the player's line of sight, and the player's arm's max reaching distance away from the player's shoulder
            //Debug.Log("CANT REACH GUn");

            /*
            // USING TRIG, CALCULATING THE NEW GUN POSITION (NOT RIGHT ANGLE TRIANGLES)
            Triangle 1      (larger one, where C is the mouse position and values are known)
               A----b-----C2-------b-------C
               |        /            /
               c     a2       a
               |   /   /
               B


            Triangle 2 (smaller one, where C2 is the new gun position)
               A-----b-----C2
               |        /
               c     a2
               |  /
               B
            */

            // 1 calculate sides a,b,c
            float a = (_frontShoulder.position - _aimTarget.position).magnitude;
            float b = (_aimOrigin.position - _aimTarget.position).magnitude;
            float c = (_frontShoulder.position - _aimOrigin.position).magnitude;

            // 2 calculate angle A
            float A = Mathf.Rad2Deg * (Mathf.Acos((Mathf.Pow(a, 2) - (Mathf.Pow(b, 2) + Mathf.Pow(c, 2))) / (-2 * b * c)));

            // drawing rays to display aiming lines
            Debug.DrawLine(_aimOrigin.position, _aimTarget.position, Color.red);
            Debug.DrawLine(_aimOrigin.position, _frontShoulder.position, Color.red);
            Debug.DrawLine(_frontShoulder.position, _aimTarget.position, Color.red);


            /* TRIANGLE 2        (smaller one, where C is the new gun position)
            // GOAL - Calculate A's interior angle to use in solving triangle 2

            Triangle 2 

            A pos = aim origin (player's eyes)     
               A=$----b----C2    C2 pos = ? what to solve for (new gun pos)
               |        /
               c=$   a2=$
               |  /
               B
            B pos = shoulder (arm origin)
            */

            // use A's inside angle to determine the side lengths of the smaller triangle
            //1 calculate side length(a2)
            float a2 = _maxHoldingDistance;
            

            /*
                    //ADDED SECTION
            //makes the player hold the gun closer/further away, depending on how far mouse is from player
            a2 = _minHoldingDistance;
            float x = _maxHoldingDistance - _minHoldingDistance;
            //range
            float _minMouseRange = _maxHoldingDistance;
            float _maxMouseRange = 4;
            x *= (_aimOrigin.position - _aimTarget.position).magnitude / _maxMouseRange;
            if (x>1){x=1;}
            a2 += x;
            */


            //2 calculate angle (C)
            // ASin((Sin(A)/a)*c) = C
            float C2 = Mathf.Rad2Deg * (Mathf.Asin(((Mathf.Sin(Mathf.Deg2Rad * A)) / a2) * c));

            //3 calculate angle (B) using angle A and C
            // 180 - A - C = B
            float B = 180 - A - C2;


            Vector2 pos1 = Vector3.up;
            Vector2 pos2 = Vector3.Normalize(_aimOrigin.position - _frontShoulder.position);

            Vector2 pos3 = Vector3.up;
            Vector2 pos4 = Vector3.Normalize(_aimTarget.position - _aimOrigin.position);

            float shoulderToEyeAngle = Vector2.Angle(pos1, pos2);
            float eyeToGunAngle = Vector2.Angle(pos3, pos4);

            /*  Debug.Log("shoulderToEyeAngle= " + shoulderToEyeAngle);
             Debug.Log("pos3= " + pos3);
             Debug.Log("pos4= " + pos4);
             Debug.Log("eyeToGunAngle= " + eyeToGunAngle);
             */


            if (_facingRight)
            {
                if (shoulderToEyeAngle < eyeToGunAngle)
                {
                    //not aiming above his eyes
                    Debug.Log("RIGHT FACING NORMAL SHOT");
                    B *= -1;
                }
                else
                {//
                    Debug.Log("RIGHT FACING ABOVE SHOT");
                }
                B *= -1;
            }
            else
            {
                if (shoulderToEyeAngle < eyeToGunAngle)
                {
                    //not aiming above his eyes
                    Debug.Log("LEFT FACING NORMAL SHOT");
                    B *= -1;
                }
                else
                {
                    Debug.Log("LEFT FACING ABOVE SHOT");
                }
            }

            //3 calculate new gun position
            Vector2 shoulderToOrigin = _aimOrigin.position - _frontShoulder.position;
            Vector2 newGunPos;

            newGunPos = Quaternion.Euler(0, 0, -B) * shoulderToOrigin;
            newGunPos = Vector3.Normalize(newGunPos) * a2;

            newGunPos += new Vector2(_frontShoulder.position.x, _frontShoulder.position.y);
            // drawing line to display side a2
            Debug.DrawLine(_frontShoulder.position, newGunPos, Color.blue);
            Debug.DrawLine(_frontShoulder.position, new Vector2(_frontShoulder.position.x + _maxHoldingDistance, _frontShoulder.position.y), Color.magenta);

            gunPos = newGunPos;
        }


        //moves the gun to the new point in worldspace
        _gun.position = gunPos;

        //aims the gun in the correct direction
        Vector2 gunDirection = _aimTarget.position - _aimOrigin.position;
        _gun.rotation = Quaternion.LookRotation(Vector3.forward, gunDirection);

        #endregion
    }

    void AnimateBody()
    {
        MoveHead();
        MoveArms();
        _animator.SetFloat("AbsSpeed", Mathf.Abs(_rigidbody.velocity.x));
        _animator.SetFloat("MoveSpeed", _rigidbody.velocity.x / 6 * transform.localScale.x);
    }

    void MoveHead(){
        //method aims the character's head toward the mouse cursor

        // converts the mouse position to a point in the game's worldspace
        Vector2 cameraWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //aims the gun in the correct direction
        Vector2 headDirection = _aimTarget.position - _aimOrigin.position;
        _head.rotation = Quaternion.LookRotation(Vector3.forward, headDirection);
        _head.transform.Rotate(0,0,90);


    }
    void MoveArms()
    {  
        MoveFrontArm();
        //MoveBackArm();
    }
    void MoveFrontArm()
    {
        // method moves arms to match gun's position

        if (_frontArmFullLength < ((_frontShoulder.position - _frontHandle.position).magnitude))
        {
            //if the gun handle is too far from the player's shoulder, exit method because arm cant reach handle
            Debug.Log("Gun is too far for the arms");
            return;
        }

        //moving front arm (trigger arm)
        Vector2 pos1 = Vector3.down;
        Vector2 pos2 = Vector3.Normalize(_frontHandle.position - _frontShoulder.position);
        float shoulderToGunAngle = Vector2.Angle(pos1, pos2);

        /* use trig to calculate arm angles

            shoulder-> A------b------C <- gun handle
                        \           /
            upper arm -> c        a  <- forarm
                          \      /
                             B <- elbow
        */

        // 1 calculate sides a,b,c
        float a = _frontForearmLength;
        float b = (_frontShoulder.position - _frontHandle.position).magnitude;
        float c = _frontUpperArmLength;

        //Debug.Log("a = " + a + ", b = " + b + ", c = " + c);

        // 2 calculate angle A (shoulder angle)
        float A = Mathf.Rad2Deg * (Mathf.Acos((Mathf.Pow(a, 2) - (Mathf.Pow(b, 2) + Mathf.Pow(c, 2))) / (-2 * b * c)));

        // 3 calculate angle B (elbow angle)
        float B = Mathf.Rad2Deg * (Mathf.Acos((Mathf.Pow(b, 2) - (Mathf.Pow(c, 2) + Mathf.Pow(a, 2))) / (-2 * c * a)));

        Debug.Log("A = " + A + " " + "   B = " + B);
        Debug.Log("shoulderToGunAngle = " + shoulderToGunAngle);

        if (_facingRight)
        {
            if (A > 0)
            {
                A *= -1;
            }
            if (B < 0)
            {
                B *= -1;
            }
            if (shoulderToGunAngle < 0)
            {
                shoulderToGunAngle *= -1;
            }
        }
        else
        {
            if (A < 0)
            {
                A *= -1;
            }
            if (B > 0)
            {
                B *= -1;
            }
            if (shoulderToGunAngle > 0)
            {
                shoulderToGunAngle *= -1;
            }
        }

        Debug.Log("A = " + A);
        Debug.Log("B = " + B);

        A += shoulderToGunAngle;
        B = 180 - B;
        B = A + B;

        Debug.Log("nA = " + A);
        Debug.Log("nB = " + B);

        _frontShoulder.eulerAngles = new Vector3(0, 0, A);

        _frontElbow.eulerAngles = new Vector3(0, 0, B);


        // drawing rays to display aiming lines
        Debug.DrawLine(_frontElbow.position, _frontWrist.position, Color.green);
        Debug.DrawLine(_frontShoulder.position, _frontWrist.position, Color.green);
        Debug.DrawLine(_frontShoulder.position, _frontElbow.position, Color.green);


    }

    void Move()
    {
        int horizontalInput = 0;
        if (Input.GetKey(KeyCode.A))
        {
            horizontalInput += -1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontalInput += 1;
        }
        if (horizontalInput != 0)
        {
            _rigidbody.AddForce(new Vector2(horizontalInput * _moveSpeed, 0));
        }

    }

}
