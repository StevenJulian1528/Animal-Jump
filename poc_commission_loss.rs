
// PoC: Commission Loss due to Rounding Errors
// This script simulates a scenario where an operator loses commission revenue
// due to frequent small reward distributions (dusting).

#[derive(Debug, Clone)]
struct StakingContract {
    principal: u64,
    commission_percentage: u64,
    total_active_stake: u64,
    pool_balance: u64,
}

impl StakingContract {
    fn new(initial_stake: u64, commission: u64) -> Self {
        StakingContract {
            principal: initial_stake,
            commission_percentage: commission,
            total_active_stake: initial_stake,
            pool_balance: initial_stake,
        }
    }

    fn add_rewards(&mut self, amount: u64) {
        self.pool_balance += amount;
        // In the real contract, this increases the value of shares,
        // effectively increasing total_active_stake value implicitly.
        self.total_active_stake += amount;
    }

    // Logic from `request_commission_internal` and `get_staking_contract_amounts_internal`
    fn request_commission(&mut self) -> u64 {
        // accumulated_rewards = total_active_stake - principal
        // In this simplified model, pool_balance represents the total worth
        let accumulated_rewards = if self.pool_balance > self.principal {
            self.pool_balance - self.principal
        } else {
            0
        };

        // Move implementation:
        // let commission_amount = accumulated_rewards * staking_contract.commission_percentage / 100;
        let commission_amount = accumulated_rewards * self.commission_percentage / 100;

        if commission_amount > 0 {
            // "Unlock" commission
            self.principal = self.pool_balance - commission_amount;
            // In reality, commission is moved out or distributed.
            // Here we just deduct it from the "stake pool" to simulate extraction
            self.pool_balance -= commission_amount;
            self.total_active_stake -= commission_amount;
        }

        commission_amount
    }
}

fn main() {
    println!("--- Verification: Commission Loss due to Rounding ---");

    let initial_stake = 1_000_000;
    let commission_rate = 10; // 10%

    // Scenario 1: Bulk Update
    // 1000 rewards accumulated, then 1 commission request.
    let mut contract_bulk = StakingContract::new(initial_stake, commission_rate);
    let total_rewards = 1000;
    contract_bulk.add_rewards(total_rewards);
    let bulk_commission = contract_bulk.request_commission();

    println!("Scenario 1 (Bulk): Total Rewards = {}, Commission = {}", total_rewards, bulk_commission);

    // Scenario 2: Frequent Updates (Dusting)
    // 1 reward added, then commission requested. Repeat 1000 times.
    let mut contract_dust = StakingContract::new(initial_stake, commission_rate);
    let mut total_dust_commission = 0;

    for _ in 0..1000 {
        contract_dust.add_rewards(1); // Add 1 unit of reward
        // If logic is `1 * 10 / 100`, it is `0.1` -> `0`.
        total_dust_commission += contract_dust.request_commission();
    }

    println!("Scenario 2 (Dust): Total Rewards = {}, Commission = {}", total_rewards, total_dust_commission);

    let loss = bulk_commission - total_dust_commission;
    if loss > 0 {
        println!("[VERIFIED] CRITICAL: Operator lost {} units of commission due to rounding.", loss);
        println!("This confirms that an attacker can deny operator revenue by triggering frequent commission updates.");
    } else {
        println!("[FAILED] No loss detected.");
    }
}
