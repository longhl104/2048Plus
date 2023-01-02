using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Assets.Scripts.Utils;
using Assets.Scripts;

public class InstructionGameManager : SwipeDetection
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private ExplosiveBlock _explosiveBlockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = .2f;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _restartButton;
    [SerializeField] private GameObject _cheeringText;
    [SerializeField] private AudioSource _gameOverSound;
    [SerializeField] private AudioSource _explosiveSound;
    [SerializeField] private TextMeshProUGUI _instructionText;
    [SerializeField] private GameObject _playButton;
    [SerializeField] private SwipeInstruction _swipeInstruction;

    private List<Node> _nodes;
    private List<BaseBlock> _blocks;
    private GameState _state;
    private int _round;
    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);
    private Board _board;
    private bool _isCheeringTextPlaying = false;
    private readonly string[] _cheeringWords = new string[] {
        "Nice!",
        "Amazing!",
        "Good Job!",
        "Brilliant!",
        "Fabulous!",
        "Fantastic!",
        "Great!",
        "Impressive!",
        "Marvellous!",
        "Outstanding!",
        "Superb!",
        "Smahsing!",
        "Terrific!",
        "Wonderful!"
    };

    private readonly string[] _explosingWords = new string[] {
        "BOOM!",
        "BANG!",
    };

    void Start()
    {
        ChangeState(GameState.GenerateLevel);
    }

    private void PlayCheeringText(bool isExplosive)
    {
        if (!_isCheeringTextPlaying)
        {
            _isCheeringTextPlaying = true;
            float duration = 0.7f;
            string text;
            if (isExplosive)
                text = _explosingWords.OrderBy(x => UnityEngine.Random.value).First();
            else
                text = _cheeringWords.OrderBy(x => UnityEngine.Random.value).First();

            _cheeringText.GetComponentInChildren<TextMeshPro>().SetText(text);
            var textObject = Instantiate(_cheeringText, new Vector2(1.5f, 1.5f), Quaternion.identity);

            var sequence = DOTween.Sequence();
            if (!isExplosive)
            {
                sequence.Append(textObject.transform.DOMove(new Vector2(1.5f, 4f), duration));
                sequence.Join(textObject.transform.DOScale(new Vector2(4.5f, 4.5f), duration));
                sequence.Append(textObject.transform.DOShakeScale(1));
            }
            else
            {
                sequence.Append(textObject.transform.DOScale(new Vector2(4.5f, 4.5f), 0.1f));
                sequence.Join(textObject.transform.DOMove(new Vector2(1.5f, 4f), 0.1f));
                sequence.Append(textObject.transform.DOShakeScale(duration));
            }

            sequence.OnComplete(() =>
            {
                Destroy(textObject);
                _isCheeringTextPlaying = false;
            });
        }
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;
        switch (newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;

            case GameState.SpawningBlocks:
                SpawnBlocks();
                break;

            case GameState.Explosing:
                break;

            case GameState.WaitingInput:
                break;

            case GameState.Moving:
                break;

            case GameState.Win:
                break;

            case GameState.Lose:
                ShowGameOver();
                break;

            default:
                break;
        }
    }

    private void ShowGameOver()
    {
        _gameOverSound.Play();
        _gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
        _restartButton.SetActive(true);
        _playButton.SetActive(true);
    }

    private void Update()
    {
        if (!_swipeInstruction.IsPlaying())
        {
            switch (_round)
            {
                case 1:
                    _swipeInstruction.Swipe(Vector2.down);
                    break;

                case 2:
                    _swipeInstruction.Swipe(Vector2.right);
                    break;

                case 3:
                    _swipeInstruction.Swipe(Vector2.left);
                    break;

                default:
                    break;
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SwipeUpEvent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SwipeDownEvent();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwipeLeftEvent();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwipeRightEvent();
        }
    }

    private void GenerateGrid()
    {
        _gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        _restartButton.SetActive(false);
        _playButton.SetActive(false);
        _round = 0;
        _nodes = new List<Node>();
        _blocks = new List<BaseBlock>();
        _instructionText.SetText("Swipe DOWN to combine 2 blocks of the SAME value");
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);
        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);
        Camera.main.transform.position = new Vector3(center.x, center.y, -10);
        ChangeState(GameState.SpawningBlocks);
    }

    private IEnumerable<Node> GetFreeNodes()
    {
        return _nodes.Where(n => n.OccupiedBlock == null).OrderBy(x => UnityEngine.Random.value);
    }

    private bool IsBoardValid()
    {
        if (_board == null)
            return false;

        if (_board.Blocks.Count > 0
            && _board.ExplosiveValue >= 2 && IsPowerOfTwo(_board.ExplosiveValue)
            )
        {
            return true;
        }

        StorageHandler.DeleteData(StorageKeys.BOARD);
        return false;

        static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }
    }

    private void SpawnBlocks()
    {
        var freeNodes = GetFreeNodes();
        int normalBlockValue = UnityEngine.Random.value > 0.75f ? 4 : 2;
        if (freeNodes.Count() > 0)
        {
            if (_round++ == 0)
            {
                _board = new Board();
                _board.ExplosiveValue = 2;
                SpawnBlock(GetNodeAtPosition(new Vector2(0, 0)), 2, _blockPrefab);
                SpawnBlock(GetNodeAtPosition(new Vector2(0, 1)), 2, _blockPrefab);
                SpawnBlock(GetNodeAtPosition(new Vector2(2, 1)), 8, _blockPrefab);
                SpawnBlock(GetNodeAtPosition(new Vector2(2, 0)), 4, _explosiveBlockPrefab);
            }
            else
            {
                if (_round == 2)
                {
                    SpawnBlock(GetNodeAtPosition(new Vector2(3, 0)), 2, _blockPrefab);
                    _instructionText.SetText("The BLACK block is IMMOVABLE, try to combine it with another block of the SAME value by swiping RIGHT");
                }

                if (_round == 3)
                {
                    SpawnBlock(GetNodeAtPosition(new Vector2(2, 1)), 4, _blockPrefab);
                    _instructionText.SetText("Now the BLACK block is EXPLOSIVE, swipe LEFT to see the effect");
                }

                if (_round == 4)
                {
                    SpawnBlock(GetNodeAtPosition(new Vector2(2, 1)), 2, _blockPrefab);
                    SpawnBlock(GetNodeAtPosition(new Vector2(2, 3)), 8, _explosiveBlockPrefab);
                    _instructionText.SetText("A new BLOCK block is spawned, its value is twice the original value of the previous EXPLODED BLACK block");
                }
            }
        }

        if (IsLoseState())
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(GameState.WaitingInput);
    }

    void SpawnBlock(Node node, int value, BaseBlock blockPrefab)
    {
        var block = Instantiate(blockPrefab, node.Pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    private bool IsLoseState()
    {
        if (_blocks.Count != _nodes.Count)
            return false;

        return _blocks.All(block =>
        {
            var adjacentBlocks = _blocks
                .Where(b => (int)(Mathf.Abs(b.Pos.x - block.Pos.x) + Mathf.Abs(b.Pos.y - block.Pos.y)) == 1)
                .ToList();

            return adjacentBlocks.All(b => b.Value != block.Value);
        });
    }

    void Shift(Vector2 dir)
    {
        if (_state != GameState.WaitingInput)
            return;

        if (_round == 1 && dir != Vector2.down)
            return;

        if (_round == 2 && dir != Vector2.right)
            return;

        if (_round == 3 && dir != Vector2.left)
            return;

        if (_round == 4)
        {
            _swipeInstruction.Stop();
            ChangeState(GameState.Lose);
            _instructionText.SetText("Try to survive as many rounds as you can. Good luck!");
            return;
        }

        _swipeInstruction.Stop();
        ChangeState(GameState.Moving);
        IEnumerable<BaseBlock> orderedBlocks = _blocks.Where(b => b is Block);
        if (dir == Vector2.up)
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.x).ThenBy(b => -b.Pos.y);
        else if (dir == Vector2.down)
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y);
        else if (dir == Vector2.left)
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.y).ThenBy(b => b.Pos.x);
        else // dir == Vector2.right
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.y).ThenBy(b => -b.Pos.x);

        var hasMovedBlocks = false;
        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                block.SetBlock(next);
                var possibleNode = GetNodeAtPosition(next.Pos + dir);
                if (possibleNode != null
                    && block is Block
                    )
                {
                    // block in possible node <- current block
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        hasMovedBlocks = true;
                        block.MergeBlock(possibleNode.OccupiedBlock);
                    }
                    else if (possibleNode.OccupiedBlock == null)
                    {
                        hasMovedBlocks = true;
                        next = possibleNode;
                    }
                }
            } while (next != block.Node);
        }

        var sequence = DOTween.Sequence();
        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
        }

        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock, block);
            }

            if (_state != GameState.Explosing)
                ChangeState(hasMovedBlocks ? GameState.SpawningBlocks : GameState.WaitingInput);
        });
    }

    void MergeBlocks(BaseBlock baseBlock, BaseBlock mergingBlock)
    {
        if (baseBlock is Block)
        {
            SpawnBlock(baseBlock.Node, baseBlock.Value * 2, _blockPrefab);
            RemoveBlock(baseBlock);
            RemoveBlock(mergingBlock);
        }
        else
        {
            PlayCheeringText(false);
            if (baseBlock.Value / 2 > 1)
            {
                SpawnBlock(baseBlock.Node, baseBlock.Value / 2, _explosiveBlockPrefab);
                RemoveBlock(baseBlock);
                RemoveBlock(mergingBlock);
            }
            else // Destroy all surrouding blocks 
            {
                _explosiveSound.Play();
                RemoveBlock(mergingBlock);
                ExplosiveBlock eb = (ExplosiveBlock)baseBlock;
                ChangeState(GameState.Explosing);
                eb.TriggerExplosion(() =>
                {
                    PlayCheeringText(true);
                    var totalScore = 0;
                    foreach (var node in _nodes.Where(n => n.OccupiedBlock != null
                         && Math.Max(Math.Abs(n.Pos.x - baseBlock.Pos.x), Math.Abs(n.Pos.y - baseBlock.Pos.y)) <= 1
                        ))
                    {
                        totalScore += node.OccupiedBlock.Value;
                        RemoveBlock(node.OccupiedBlock);
                    }

                    RemoveBlock(baseBlock);
                    ChangeState(GameState.SpawningBlocks);
                });
            }
        }
    }

    void RemoveBlock(BaseBlock block)
    {
        _blocks.Remove(block);
        if (block is Block b)
        {
            StartCoroutine(b.Destroy());
        }
        else
        {
            if (block.gameObject != null)
                Destroy(block.gameObject);
        }
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

    protected override void SwipeUpEvent()
    {
        Shift(Vector2.up);
    }

    protected override void SwipeDownEvent()
    {
        Shift(Vector2.down);
    }

    protected override void SwipeLeftEvent()
    {
        Shift(Vector2.left);
    }

    protected override void SwipeRightEvent()
    {
        Shift(Vector2.right);
    }

    public void Restart()
    {
        while (_blocks.Count > 0)
        {
            RemoveBlock(_blocks.First());
        }

        ChangeState(GameState.GenerateLevel);
    }

    public void OnBackButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}