using UnityEngine;
using System.Collections;
using Lidgren.Network;

/// <summary>
/// Character class
/// 
/// This class is passed around.
/// It holds the position, name ( not used in this example ) ( even thou it gets sent all over )
/// Connection (ip+port)
/// 
/// </summary>
public class Player : MonoBehaviour
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool isMine;
    public NetConnection Connection { get; set; }

    public float movementSpeed = 0.25f;

    private float _velocityX;
    private float _velocityY;
    private Vector2 _realPosition;

    //Physics and movement properties units/second
    float acceleration = 1f;
    float maxSpeed = 5f;
    float gravity = 0.2f;
    float maxFall = 200f;
    float jumpVelocity = 18f;

    bool lastInput;
    float jumpPressedTime;
    float jumpPressLeeway = 0.1f;

    int layerMask;

    Rect box;

    Vector2 velocity;

    bool grounded = false;

    int horizontalRays = 6;
    int verticalRays = 4;

    void Start()
    {
        layerMask = 1 << LayerMask.NameToLayer("normalCollisions");
        _realPosition = gameObject.transform.position;
    }

    void FixedUpdate()
    {
        if (isMine)
        {
            Bounds bounds = GetComponent<Collider2D>().bounds;
            box = new Rect(
                bounds.min.x,
                bounds.min.y,
                bounds.size.x,
                bounds.size.y
            );

            if (!grounded)
                velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - gravity, -maxFall));

            if (grounded || velocity.y < 0)
            {
                Vector2 startPoint = new Vector2(bounds.min.x, bounds.center.y);
                Vector2 endPoint = new Vector2(bounds.max.x, bounds.center.y);

                float distance = bounds.extents.y + (grounded ? 0 : Mathf.Abs(velocity.y * Time.deltaTime));
                for (int i = 0; i < verticalRays; i++)
                {
                    float lerpAmount = i / ((float)verticalRays - 1);
                    Debug.Log(i + "/ " + verticalRays + "-1 = " + lerpAmount);

                    Vector2 origin = Vector2.Lerp(startPoint, endPoint, lerpAmount);

                    RaycastHit2D hitInfo = Physics2D.Raycast(origin, Vector2.down, distance, layerMask);

                    Debug.DrawRay(origin, Vector2.down * distance, Color.red);
                    // <------------------
                    if (hitInfo)
                    {
                        grounded = true;
                        transform.Translate(Vector2.down * (hitInfo.distance - box.height / 2));
                        velocity = new Vector2(velocity.x, 0);
                        break;
                    }
                    else
                    {
                        grounded = false;
                    }
                }              
            }

            float horizontalAxis = Input.GetAxisRaw("Horizontal");

            float newVelocityX = velocity.x;
            if (horizontalAxis != 0) //Input is found and apply now to movement
            {
                newVelocityX += acceleration * horizontalAxis;
                newVelocityX = Mathf.Clamp(newVelocityX, -maxSpeed, maxSpeed);
            }
            else if (velocity.x != 0) // apply deceleration due to no input
            {
                int modifier = velocity.x > 0 ? -1 : 1;
                newVelocityX += acceleration * modifier;
            }

            velocity = new Vector2(newVelocityX, velocity.y);

            if (velocity.x != 0) //physics checks
            {
                Vector2 startPoint = new Vector2(box.center.x, box.yMin);
                Vector2 endPoint = new Vector2(box.center.x, box.yMax);

                RaycastHit2D hitinfo;

                float sideRayLenght = box.width / 2 + Mathf.Abs(newVelocityX * Time.deltaTime);
                Vector2 direction = newVelocityX > 0 ? Vector2.left : Vector2.right;

                for (int i = 0; i < horizontalRays; i++)
                {
                    float lerpAmount = (float)i / (float)(horizontalRays - 1);
                    Vector2 origin = Vector2.Lerp(startPoint, endPoint, lerpAmount);

                    hitinfo = Physics2D.Raycast(origin, direction, sideRayLenght);

                    if(hitinfo)
                    {
                        transform.Translate(direction * (hitinfo.distance - box.width / 2));
                        velocity = new Vector2(0, velocity.y);
                        break;
                    }

                }
            }

            //float HorizontalAxis = Input.GetAxis("Horizontal");
            //transform.position = new Vector2(transform.position.x + (HorizontalAxis * 0.25f), transform.position.y);
        
            //        if(HorizontalAxis != _velocityY)
            //            NetOutgoingMessageMovePlayer();

            //        _velocityX = HorizontalAxis;
            //        _velocityY = 0f;
        }
        else
        {
            float xMove = _velocityX * movementSpeed * Time.deltaTime;
            float yMove = _velocityY * movementSpeed * Time.deltaTime;

            if (xMove < 0.001 && xMove > 0 || xMove < 0 && xMove > -0.001)
                xMove = 0;
            if (yMove < 0.001 && yMove > 0 || yMove < 0 && yMove > -0.001)
                yMove = 0;

            Vector2 movedistance = new Vector2(xMove * 100, yMove * 100); //Schiet over!

            //Vector2 curPositon = gameObject.transform.position;
            //curPositon += movedistance;

            _realPosition += movedistance;

            transform.position = Vector3.Lerp(gameObject.transform.position, _realPosition, Time.deltaTime * 3);
        }


    }


    void Update()
    {
        if (Input.GetButtonDown("Jump") && grounded)
        {
            velocity = new Vector2(velocity.x, jumpVelocity);
        }
    }
    
    void LateUpdate()
    {
        transform.Translate(velocity * Time.deltaTime);
    }

    //void Update()
    //{
    //    if (isMine)
    //    {
    //        float HorizontalAxis = Input.GetAxis("Horizontal");
    //        float VerticalAxis = Input.GetAxis("Vertical");

    //        transform.position = new Vector2(transform.position.x + (HorizontalAxis * movementSpeed), transform.position.y + (VerticalAxis * 0.25f));
            
    //        if(HorizontalAxis != _velocityX || VerticalAxis != _velocityY)
    //            NetOutgoingMessageMovePlayer();

    //        _velocityX = HorizontalAxis;
    //        _velocityY = VerticalAxis;

    //    }
    //    else
    //    {

    //        float xMove = _velocityX * movementSpeed * Time.deltaTime;
    //        float yMove = _velocityY * movementSpeed * Time.deltaTime;

    //        if (xMove < 0.001 && xMove > 0 || xMove < 0 && xMove > -0.001)
    //            xMove = 0;
    //        if (yMove < 0.001 && yMove > 0 || yMove < 0 && yMove > -0.001)
    //            yMove = 0;

    //        Vector2 movedistance = new Vector2(xMove*100, yMove*100); //Schiet over!

    //        //Vector2 curPositon = gameObject.transform.position;
    //        //curPositon += movedistance;

    //        _realPosition += movedistance;

    //        transform.position = Vector3.Lerp(gameObject.transform.position, _realPosition, Time.deltaTime * 3);
    //    }
    //}

    public void PushUpdate(float x, float y, float xvel, float yvel, float triptime)
    {
        //Player is updating their position.
        float newx = x + movementSpeed * xvel * triptime;
        float newy = y + movementSpeed * yvel * triptime;

        //This is where we predict they are right now.
        _realPosition = new Vector2(newx, newy);

        _velocityX = xvel;
        _velocityY = yvel;
    }

    public void NetOutgoingMessageMovePlayer()
    {
        NetOutgoingMessage netOutgoingMessage = ServerConnection.CreateNetOutgoingMessage();
        netOutgoingMessage.Write((byte)PackageTypes.PlayerMovement);
        netOutgoingMessage.Write((Vector2)transform.position);
        netOutgoingMessage.Write(_velocityX);
        netOutgoingMessage.Write(_velocityY);
        ServerConnection.SendNetOutgoingMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered, 10);
    }

    public void NetIncomingMessageMovePlayer(NetIncomingMessage netIncomingMessage)
    {
        Vector2 playerPosition = netIncomingMessage.ReadVector2();
        _velocityX = netIncomingMessage.ReadFloat();
        _velocityY = netIncomingMessage.ReadFloat();

        float triptime = netIncomingMessage.ReadFloat() + ServerConnection.Roundtriptime;
        
        PushUpdate(playerPosition.x, playerPosition.y, _velocityX, _velocityY, triptime);
    }

    void OnCollisionEnter(Collision col)
    {
        DebugConsole.Log("I HAVE BEEN HIT!");
    }
}
