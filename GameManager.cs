using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Experimental.UIElements;
using UnityEngine.UI;
using System.Threading;
using System.Net.Sockets;
using System;
using Image = UnityEngine.Experimental.UIElements.Image;

[Serializable]
public class GameManager : MonoBehaviour
{ 
    public Board mBoard;
    public Text info;
    [HideInInspector]
    public bool host;

    public bool b = false;
    public string p = null;

    public PieceManager mPieceManager;
    [HideInInspector]
    public ConnectionClient cc;
    [HideInInspector]
    public ConnectionServer cs;
    
    private Thread connectionThread;
    public List<string> positions;
    public Cell previous;
    public string previousP = null;

    private Dictionary<string, Type> mPieceLibrary = new Dictionary<string, Type>()
    {
        {"Pawn",  typeof(Pawn)},
        {"Rook",  typeof(Rook)},
        {"Knight", typeof(Knight)},
        {"Bishop",  typeof(Bishop)},
        {"King",  typeof(King)},
        {"Queen",  typeof(Queen)}
    };

    void Update()
    {
        if (b)
        {
            ReplacePieces();
            b = false;
        }
        switch (p)
        {
            case "CLIENTSEND":
                mPieceManager.SetInteractive(mPieceManager.mWhitePieces, false);
                mPieceManager.SetInteractive(mPieceManager.mBlackPieces, false);
                break;
            case "CLIENTRECEIVE":
                mPieceManager.SetInteractive(mPieceManager.mWhitePieces, false);
                mPieceManager.SetInteractive(mPieceManager.mBlackPieces, true);
                break;
            case "SERVERSEND":
                mPieceManager.SetInteractive(mPieceManager.mWhitePieces, false);
                mPieceManager.SetInteractive(mPieceManager.mBlackPieces, false);
                break;
            case "SERVERRECEIVE":
                mPieceManager.SetInteractive(mPieceManager.mWhitePieces, true);
                mPieceManager.SetInteractive(mPieceManager.mBlackPieces, false);
                break;
            default:
                break;
        }
        p = null;
        
    }

    public void MakeBoard()
    {
        mBoard.Create();
        mPieceManager.Setup(mBoard, this);

        /*if (host)
        {
            cs = new ConnectionServer();
            cs.prepareConnection();
        } else
        {
            cc = new ConnectionClient();
            //get connection info
            cc.connect();
        }*/
        

        
    }

    public List<string> generateBoardList(Board b)
    {
        List<string> s = new List<string>();
        for(int i=0; i < 8; i++) {
            for(int j=0; j<8; j++)
            {
                //Debug.Log(b.mAllCells[i, j].mCurrentPiece.GetComponent<UnityEngine.UI.Image>().sprite.name.Substring(2));
                if (b.mAllCells[i, j].mCurrentPiece == null)
                {
                    s.Add("empty");
                } else
                {
                    //Debug.Log(b.mAllCells[i, j].mCurrentPiece.GetComponent<UnityEngine.UI.Image>().sprite.name.Substring(2) + "," + b.mAllCells[i, j].mCurrentPiece.mColor);
                    s.Add(b.mAllCells[i, j].mCurrentPiece.GetComponent<UnityEngine.UI.Image>().sprite.name.Substring(2)+"|"+b.mAllCells[i,j].mCurrentPiece.mColor);
                }
            }
        }
        s.Add(previousP);
        return s;
    }

    public void PlacePieces(List<string> pieces)
    {
        mPieceManager.KillAllPieces();
        mPieceManager.mWhitePieces.Clear();
        mPieceManager.mBlackPieces.Clear();
        int count = 0;
        int kingCount = 0;
        bool whiteKingDead = true;
        bool blackKingDead = true;
        for (int i = 0; i < 8; i++)
        {
            for(int j=0; j<8; j++)
            {
                if (!pieces[count].Equals("empty"))
                {
                    string[] s = pieces[count].Split('|');
                    
                    string key = s[0];
                    string color = s[1];
                    if(color.Equals("RGBA(1.000, 1.000, 1.000, 1.000)"))
                    {
                        color = "White";
                    }
                    else
                    {
                        color = "Black";
                    }
                    Type pieceType = mPieceLibrary[key];
                    BasePiece bp = mPieceManager.CreatePiece(pieceType);
                    Color32 spriteColor;
                    if (key.Equals("Pawn"))
                    {
                        if (color.Equals("Black") && j != 6)
                        {
                            bp.mIsFirstMove = false;
                        }
                        else if(color.Equals("White") && j != 1)
                        {
                            bp.mIsFirstMove = false;
                        }
                    }
                    if (key.Equals("King"))
                    {
                        kingCount++;
                        if (color.Equals("Black"))
                        {
                            blackKingDead = false;
                        }
                        else if (color.Equals("White"))
                        {
                            whiteKingDead = false;
                        }
                    }
                    if (color.Equals("Black"))
                    {
                        spriteColor = new Color32(0, 0, 0, 255);
                        bp.Setup(Color.black, spriteColor, mPieceManager);
                        mPieceManager.mBlackPieces.Add(bp);
                    } else
                    {
                        spriteColor = new Color32(255, 255, 255, 255);
                        bp.Setup(Color.white, spriteColor, mPieceManager);
                        mPieceManager.mWhitePieces.Add(bp);
                    }
                    bp.mCurrentCell = mBoard.mAllCells[i, j];
                    bp.mOriginalCell = mBoard.mAllCells[i, j];
                    bp.mCurrentCell.mCurrentPiece = bp;
                    bp.transform.position = mBoard.mAllCells[i, j].transform.position;
                    bp.gameObject.SetActive(true);
                    //mBoard.mAllCells[i, j].transform.position = bp.transform.position;
                } else
                {
                    mBoard.mAllCells[i, j].mCurrentPiece = null;
                }
                count++;
            }
        }
        if (kingCount < 2)
        {
            if (whiteKingDead)
            {
                info.text = "Black wins!";
            }
            else if (blackKingDead)
            {
                info.text = "White wins!";
            }
            p = "CLIENTSEND";
            if (IsHost())
            {
                cs.SendMessage();
            }
            else
            {
                cc.SendMessage();
            }
        }
    }

    public void Connect()
    {
        ConnectToOtherPlayer();
    }

    public void quit()
    {
        Application.Quit();
    }

    private void ConnectToOtherPlayer()
    {
        /*try
        {
            connectionThread = new Thread(new ThreadStart(establishConnection));
            connectionThread.IsBackground = true;
            connectionThread.Start();

        } catch(Exception e)
        {
            Debug.Log("Uhhh");
        }*/
    }

    public void establishConnection()
    {
        
    }

    public bool IsHost()
    {
        return host;
    }

    public void setHost(int n)
    {
        if (n == 1)
        {
            host = true;
        } else
        {
            host = false;
        }
    }

    public void ReplacePieces()
    {
        PlacePieces(positions);
    }
}
