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
    float acceleration = 4f;
    float maxSpeed = 150f;
    float gravity = 0.1f;
    float maxFall = 200f;
    float jump = 200f;

    int layerMask;

    Rect box;

    Vector2 velocity;

    bool grounded = false;
    bool falling = false;

    int horizontalRays = 6;
    int verticalRays = 4;
    float margin = 0.5f;

    void Start()
    {
        layerMask = 1 << LayerMask.NameToLayer("normalCollisions");
        _realPosition = gameObject.transform.position;
    }

    //void FixedUpdate()
    //{
    //    Bounds bounds = GetComponent<Collider2D>().bounds;
    //    box = new Rect(
    //        bounds.min.x,
    //        bounds.min.y,
    //        bounds.size.x,
    //        bounds.size.y
    //    );

    //    if (!grounded)
    //        velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - gravity, -maxFall));

    //    if (velocity.y < 0)
    //        falling = true;

    //    if (grounded || falling)
    //    {
    //        Vector2 startPoint = new Vector2(box.xMin + margin, box.center.y);
    //        Vector2 endPoint = new Vector2(box.xMax - margin, box.center.y);

    //        RaycastHit2D hitInfo;

    //        float distance = box.height / 2 + (grounded ? margin : Mathf.Abs(velocity.y * Time.deltaTime));

    //        for (int i = 0; i < verticalRays; i++)
    //        {
    //            float lerpAmount = (float)i / (float)verticalRays - 1;
    //            Vector2 origin = Vector2.Lerp(startPoint, endPoint, lerpAmount);
    //            Ray2D ray = new Ray2D(origin, Vector2.down);

    //            hitInfo = Physics2D.Raycast(origin, Vector2.down, distance, layerMask);

    //            if (hitInfo)
    //            {
    //                grounded = true;
    //                falling = false;
    //                transform.Translate(Vector2.down * (hitInfo.distance - box.height / 2));
    //                velocity = new Vector2(velocity.x, 0);
    //                break;
    //            }
    //            else
    //            {
    //                grounded = false;
    //            }
    //        }

    //        //if (!hitInfo)
    //        //{
    //        //    grounded = false;
    //        //}
    //    }


    //}

    void LateUpdate()
    {
        transform.Translate(velocity * Time.deltaTime);
    }

    void Update()
    {
        if (isMine)
        {
            float HorizontalAxis = Input.GetAxis("Horizontal");
            float VerticalAxis = Input.GetAxis("Vertical");

            transform.position = new Vector2(transform.position.x + (HorizontalAxis * movementSpeed), transform.position.y + (VerticalAxis * 0.25f));
            
            if(HorizontalAxis != _velocityX || VerticalAxis != _velocityY)
                NetOutgoingMessageMovePlayer();

            _velocityX = HorizontalAxis;
            _velocityY = VerticalAxis;

        }
        else
        {

            float xMove = _velocityX * movementSpeed * Time.deltaTime;
            float yMove = _velocityY * movementSpeed * Time.deltaTime;

            if (xMove < 0.001 && xMove > 0 || xMove < 0 && xMove > -0.001)
                xMove = 0;
            if (yMove < 0.001 && yMove > 0 || yMove < 0 && yMove > -0.001)
                yMove = 0;

            Vector2 movedistance = new Vector2(xMove*100, yMove*100); //Schiet over!

            //Vector2 curPositon = gameObject.transform.position;
            //curPositon += movedistance;

            _realPosition += movedistance;

            transform.position = Vector3.Lerp(gameObject.transform.position, _realPosition, Time.deltaTime * 3);
        }
    }

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
