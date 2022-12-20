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

public class GameManager : SwipeDetection
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private ExplosiveBlock _explosiveBlockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = .2f;
    [SerializeField] private bool _isTest = false;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _restartButton;

    private List<Node> _nodes;
    private List<BaseBlock> _blocks;
    private GameState _state;
    private int _round;
    private const string HIGH_SCORE_KEY = "highScore";

    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);
    private Board _board;

    void Start()
    {
        ChangeState(GameState.GenerateLevel);
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
        _gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
        _restartButton.SetActive(true);
    }

    private void Update()
    {
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

    private void SetScore(int score)
    {
        _board.Score = score;
        _scoreText.text = "Score: " + _board.Score;
        if (!PlayerPrefs.HasKey(HIGH_SCORE_KEY))
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
        }

        if (_board.Score > PlayerPrefs.GetInt(HIGH_SCORE_KEY))
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, _board.Score);
            _highScoreText.text = "High Score: " + _board.Score;
        }
    }

    private void GenerateGrid()
    {
        _highScoreText.text = "High Score: " + PlayerPrefs.GetInt(HIGH_SCORE_KEY);
        _gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        _restartButton.SetActive(false);
        _round = 0;
        _nodes = new List<Node>();
        _blocks = new List<BaseBlock>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        if (_isTest)
        {
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 0)), 2, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 1)), 4, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 2)), 8, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 3)), 16, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 3)), 32, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 3)), 64, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(3, 2)), 256, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(3, 1)), 512, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(3, 0)), 1024, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 0)), 2, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 0)), 4096, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 1)), 2, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 2)), 8, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 2)), 16, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 1)), 2, _explosiveBlockPrefab);
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
        if (freeNodes.Count() > 0)
        {
            if (_round++ == 0)
            {
                _board = (Board)StorageHandler.LoadData(StorageKeys.BOARD);
                if (!IsBoardValid())
                {
                    _board = new Board();
                    SetScore(0);
                    _board.ExplosiveValue = 2;
                    int normalBlockValue = UnityEngine.Random.value > 0.75f ? 4 : 2;
                    var freeNode = freeNodes.First();
                    SpawnBlock(freeNode, normalBlockValue, _blockPrefab);

                    if (freeNodes.Count() > 1)
                    {
                        freeNode = freeNodes.Skip(1).First();
                        SpawnBlock(freeNode, _board.ExplosiveValue, _explosiveBlockPrefab);
                    }
                }
                else
                {
                    SetScore(_board.Score);
                    foreach (var block in _board.Blocks)
                    {
                        SpawnBlock(_nodes.First(n => (int)n.Pos.x == block.X && (int)n.Pos.y == block.Y), block.Value, block.IsExplosive ? _explosiveBlockPrefab : _blockPrefab);
                    }
                }
            }
            else
            {
                SpawnBlock(freeNodes.First(), UnityEngine.Random.value > 0.75f ? 4 : 2, _blockPrefab);
                if (freeNodes.Count() > 1 && _blocks.All(b => b is Block))
                {
                    _board.ExplosiveValue *= 2;
                    SpawnBlock(freeNodes.Skip(1).First(), _board.ExplosiveValue, _explosiveBlockPrefab);
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

        ChangeState(GameState.Moving);
        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();
        if (dir == Vector2.right || dir == Vector2.up)
            orderedBlocks.Reverse();

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
            SetScore(_board.Score + baseBlock.Value * 2);
            SpawnBlock(baseBlock.Node, baseBlock.Value * 2, _blockPrefab);
            RemoveBlock(baseBlock);
            RemoveBlock(mergingBlock);
        }
        else
        {
            if (baseBlock.Value / 2 > 1)
            {
                SetScore(_board.Score + baseBlock.Value * 2);
                SpawnBlock(baseBlock.Node, baseBlock.Value / 2, _explosiveBlockPrefab);
                RemoveBlock(baseBlock);
                RemoveBlock(mergingBlock);
            }
            else // Destroy all surrouding blocks 
            {
                RemoveBlock(mergingBlock);
                ExplosiveBlock eb = (ExplosiveBlock)baseBlock;
                eb.ExploseAction = () =>
                {
                    var totalScore = 0;
                    foreach (var node in _nodes.Where(n => n.OccupiedBlock != null
                         && Math.Max(Math.Abs(n.Pos.x - baseBlock.Pos.x), Math.Abs(n.Pos.y - baseBlock.Pos.y)) <= 1
                        ))
                    {
                        totalScore += node.OccupiedBlock.Value;
                        RemoveBlock(node.OccupiedBlock);
                    }

                    SetScore(_board.Score + totalScore);
                    RemoveBlock(baseBlock);
                    ChangeState(GameState.SpawningBlocks);
                };

                ChangeState(GameState.Explosing);
                eb.TriggerExplosion();
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
            Destroy(block.gameObject);
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

        StorageHandler.DeleteData(StorageKeys.BOARD);
        ChangeState(GameState.GenerateLevel);
    }

    public void OnBackButtonClicked()
    {
        if (_board != null)
        {
            _board.Blocks = _blocks.Select(block => new Board.Block()
            {
                X = (int)block.Pos.x,
                Y = (int)block.Pos.y,
                Value = block.Value,
                IsExplosive = block is ExplosiveBlock,
            }).ToList();

            StorageHandler.SaveData(_board, StorageKeys.BOARD);
        }

        SceneManager.LoadScene("MainMenu");
    }
}

[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    Explosing,
    WaitingInput,
    Moving,
    Win,
    Lose,
}

[Serializable]
public class Board
{
    [Serializable]
    public class Block
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Value { get; set; }
        public bool IsExplosive { get; set; }
    }

    public List<Block> Blocks { get; set; } = new List<Block>();

    public int Score { get; set; }
    public int ExplosiveValue { get; set; }
}