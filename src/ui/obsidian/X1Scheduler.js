/**
 * delay - Promisified setTimeout.
 *
 * @param  {number} time Time (ms)
 * @returns {Promise} Promise that resolves after time ms
 */
const delay = time => new Promise(resolve => setTimeout(() => resolve(), time))

/**
 * retry - Recursive timeout function.
 *
 * @param {{ task, baseDelay, maxDelay, checkIsCancelled, backoff }} options Options
 * @returns {Promise} Promise
 */
let timers = {}
async function retry(getTask, baseDelay, maxDelay, checkIsCancelled, backoff, onCancelComplete) {
  
  var info = getTask()
  const toClear = timers[info.taskId]
  if (toClear) clearTimeout(toClear)
  //await delay(baseDelay)
  console.timeEnd(info.taskId)

  if (checkIsCancelled()) {
    onCancelComplete && onCancelComplete()
  } else {
    try {
      const task = getTask()
      task.task && task.task()
    } finally {
      // re-schedule next execution regardless of task result
      const nextDelay = baseDelay * backoff
      const adjustedDelay = maxDelay ? Math.min(maxDelay, nextDelay) : nextDelay

      const timer = setTimeout(
        retry,
        adjustedDelay,
        getTask,
        adjustedDelay,
        maxDelay,
        checkIsCancelled,
        backoff,
        onCancelComplete
      )
      timers[info.taskId] = timer

      console.time(info.taskId)
    }
  }
}

/**
 * createScheduler - Creates scheduler instance. It is allowed to have unlimited scheduler instances.
 *
 * Schedulers allow to add and remove tasks with flat or backoff schedules.
 *
 * @returns {*} Schedular
 */
export default function createScheduler() {
  const taskList = {}

  /**
   * onCancelComplete - Physically removes `taskDefinition` from the execution queue.
   *
   * @param {string|Function} task - either original callback or `taskId`
   */
  const onCancelComplete = task => {
    const taskDesc = findTask(task)
    if (taskDesc) {
      const keyToRemove = Object.keys(taskList).find(key => taskList[key] === taskDesc)
      if (keyToRemove) {
        delete taskList[keyToRemove]
      }
    }
  }

  /**
   * findTask - Searches for the `taskDefinition` is in the execution queue.
   *
   * @param {string|Function} task - either original callback or `taskId`
   * @returns {*} Task definition
   */
  const findTask = task => {
    // search by taskId first
    const { [task]: taskDesc } = taskList
    if (taskDesc) {
      return taskDesc
    }

    // else try to find by task callback
    return Object.values(taskList).find(entry => entry.task === task)
  }

  /**
   * addTask - Enqueues `task` for the continuous execution.
   *
   * @param {*} taskDefinition task configuration
   * @param {Function} taskDefinition.task task callback
   * @param {string} taskDefinition.taskId unique task identifier. Is required if there is an
   * intention to replace `task`
   * @param {number} taskDefinition.baseDelay base delay in ms
   * @param {number} taskDefinition.backoff delay multiplier. Is multiplies `baseDelay` to
   * produce next iteration delay. Use 1 for constant delay
   * @param {number} taskDefinition.maxDelay maximum delay. Only useful if `@backoff` is set
   */
  const addTask = ({ task, taskId, baseDelay, maxDelay, backoff = 1 }) => {
    const getTask = () => {
      if (!taskId) {
        return { task: task, taskId: taskId }
      }
      const taskDesc = findTask(taskId)
      return taskDesc && { task: taskDesc.task, taskId: taskId }
    }
    console.log('ADD TASK CALLED:' + taskId)
    const checkIsCancelled = () => !isScheduled(taskId || task)
    taskList[taskId || task] = { isCancelled: false, task }
    retry(getTask, baseDelay, maxDelay, checkIsCancelled, backoff, () => onCancelComplete(task))
  }

  /**
   * removeTask - Removes `task` from the execution queue.
   *
   * @param {string|Function} task - either original callback or `taskId`
   * @returns {boolean} Boolean indicating whether the task was removed
   */
  const removeTask = task => {
    const taskDesc = findTask(task)
    if (taskDesc) {
      taskDesc.isCancelled = true
      return true
    }
    return false
  }

  /**
   * removeAllTasks - Clears the entire execution queue.
   */
  const removeAllTasks = () => {
    Object.keys(taskList).forEach(removeTask)
  }

  /**
   * isScheduled - Checks whether `task` is in the execution queue.
   *
   * @param {string|Function} task - either original callback or `taskId`
   * @returns {boolean} Boolean indicating whether the task is scheduled
   */
  const isScheduled = task => {
    const taskDesc = findTask(task)
    return Boolean(taskDesc && !taskDesc.isCancelled)
  }

  return {
    addTask,
    removeTask,
    removeAllTasks,
    isScheduled,
    taskList,
  }
}
