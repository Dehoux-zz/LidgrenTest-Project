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

    private Vector2 _realPosition;

    //Physics and movement properties units/second
    float acceleration = 1f;
    float maxSpeed = 5f;
    float gravity = 0.2f;
    float maxFall = 200f;
    float jumpVelocity = 10f;

    bool lastInput;
    float jumpPressedTime;
    float jumpPressLeeway = 0.1f;

    int layerMask;

    Rect box;

    Vector2 velocity;
    Vector2 networkVelocity;
    Vector2 networkPlayerPosition;

    bool grounded = false;
    SpriteRenderer spriteRenderer;

    int horizontalRays = 6;
    int verticalRays = 4;

    void Start()
    {
        layerMask = 1 << LayerMask.NameToLayer("normalCollisions");
        _realPosition = gameObject.transform.position;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (isMine)
        {
            Bounds bounds = GetComponent<Collider2D>().bounds;

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

                    Vector2 origin = Vector2.Lerp(startPoint, endPoint, lerpAmount);

                    RaycastHit2D hitInfo = Physics2D.Raycast(origin, Vector2.down, distance, layerMask);
                    Debug.DrawRay(origin, Vector2.down * distance, Color.red);

                    if (hitInfo)
                    {
                        grounded = true;
                        transform.Translate(Vector2.down * (hitInfo.distance - bounds.extents.y));
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
                Vector2 startPoint = new Vector2(bounds.center.x, bounds.min.y + 0.1f);
                Vector2 endPoint = new Vector2(bounds.center.x, bounds.max.y - 0.1f);

                RaycastHit2D hitinfo;

                float distance = bounds.extents.x + Mathf.Abs(newVelocityX * Time.deltaTime);

                Vector2 direction = newVelocityX > 0 ? Vector2.right : Vector2.left;

                for (int i = 0; i < horizontalRays; i++)
                {
                    float lerpAmount = i / ((float)horizontalRays - 1);
                    Vector2 origin = Vector2.Lerp(startPoint, endPoint, lerpAmount);

                    hitinfo = Physics2D.Raycast(origin, direction, distance, layerMask);

                    Debug.DrawRay(origin, direction * distance, Color.red);

                    if (hitinfo)
                    {
                        transform.Translate(direction * (hitinfo.distance - bounds.extents.x));
                        velocity = new Vector2(0, velocity.y);
                        break;
                    }

                }
            }

            if (networkVelocity.x != velocity.x || networkVelocity.y != velocity.y)
            {
                NetOutgoingMessagePlayerMove();
            }

            //velocity.x = Mathf.Round(velocity.x * 100) / 100;
            //velocity.y = Mathf.Round(velocity.y * 100) / 100;

            networkVelocity.x = velocity.x;
            networkVelocity.y = velocity.y;

            if (grounded)
            {
                if (spriteRenderer.color == Color.green)
                {
                    spriteRenderer.color = Color.blue;
                }
            }

        }
        else
        {
            if (grounded)
            {
                if (spriteRenderer.color == Color.green)
                {
                    spriteRenderer.color = Color.red;
                }
            }
            //float xMove = networkVelocity.x * movementSpeed * Time.deltaTime;
            //float yMove = networkVelocity.y * movementSpeed * Time.deltaTime;

            //if (xMove <= movementSpeed && xMove > 0 || xMove < 0 && xMove >= -movementSpeed)
            //    xMove = 0;
            //if (yMove <= movementSpeed && yMove > 0 || yMove < 0 && yMove >= -movementSpeed)
            //    yMove = 0;

            //Vector2 movedistance = new Vector2(xMove, yMove);

            ////Vector2 curPositon = gameObject.transform.position;
            ////curPositon += movedistance;

            //_realPosition += movedistance;

            //transform.position = Vector3.Lerp(gameObject.transform.position, _realPosition, Time.deltaTime);

        }


    }


    void Update()
    {
        if (isMine && Input.GetButtonDown("Jump") && grounded)
        {
            velocity = new Vector2(velocity.x, jumpVelocity);
            spriteRenderer.color = Color.green;
            NetOutgoingMessagePlayerJump();
        }
    }

    void LateUpdate()
    {
        transform.Translate(velocity * Time.deltaTime);

        if (!isMine && velocity == Vector2.zero)
        {
            transform.position = Vector2.Lerp(gameObject.transform.position, networkPlayerPosition, Time.deltaTime * 10 * acceleration);
        }
    }

    //public Vector2 GetNetworkPositionCheck()
    //{
    //    float newx = networkPlayerPosition.x + movementSpeed * velocity.x;
    //    float newy = networkPlayerPosition.y + movementSpeed * velocity.y;
    //    return new Vector2(newx, newy);
    //}

    public void NetOutgoingMessagePlayerMove()
    {
        NetOutgoingMessage netOutgoingMessage = ServerConnection.CreateNetOutgoingMessage();
        netOutgoingMessage.Write((byte)PackageTypes.PlayerMovement);
        netOutgoingMessage.Write((Vector2)transform.position);
        netOutgoingMessage.Write(velocity);
        netOutgoingMessage.Write(grounded);
        ServerConnection.SendNetOutgoingMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered, 10);
    }

    public void NetOutgoingMessagePlayerJump()
    {
        NetOutgoingMessage netOutgoingMessage = ServerConnection.CreateNetOutgoingMessage();
        netOutgoingMessage.Write((byte)PackageTypes.PlayerJump);
        ServerConnection.SendNetOutgoingMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered, 11);
    }

    public void NetIncomingMessageMovePlayer(NetIncomingMessage netIncomingMessage)
    {
        networkPlayerPosition = netIncomingMessage.ReadVector2();
        velocity = netIncomingMessage.ReadVector2();
        grounded = netIncomingMessage.ReadBoolean();
    }

    public void NetIncomingMessageJumpPlayer(NetIncomingMessage netIncomingMessage)
    {
        spriteRenderer.color = Color.green;
        grounded = false;
    }

    void OnCollisionEnter(Collision col)
    {
        DebugConsole.Log("I HAVE BEEN HIT!");
    }
}
