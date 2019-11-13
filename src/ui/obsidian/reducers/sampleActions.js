/**  Actions
see https://redux.js.org/basics/actions
 Actions are plain JavaScript objects. Actions must have a type property that indicates the type of action being performed, e.g.
 {
   type: ADD_TODO,
   text: 'Build my first Redux app'
 }
*/

// Action Types
// Action types are string constants
export const REQUEST_POSTS = 'REQUEST_POSTS'

export const RECEIVE_POSTS = 'RECEIVE_POSTS'

export const SELECT_SUBREDDIT = 'SELECT_SUBREDDIT'

export const INVALIDATE_SUBREDDIT = 'INVALIDATE_SUBREDDIT'

// Action Creators
// Action creators simply return an action:
export const requestPosts = subreddit => ({
  type: REQUEST_POSTS,
  subreddit,
})

export const receivePosts = (subreddit, json) => ({
  type: RECEIVE_POSTS,
  subreddit,
  posts: json.data.children.map(child => child.data),
  receivedAt: Date.now(),
})

export const selectSubreddit = subreddit => {
  return {
    type: SELECT_SUBREDDIT,
    subreddit,
  }
}

export const invalidateSubreddit = subreddit => ({
  type: INVALIDATE_SUBREDDIT,
  subreddit,
})

/* In traditional Flux, action creators often trigger a dispatch when invoked, like so:
function addTodoWithDispatch(text) {
  const action = {
    type: ADD_TODO,
    text
  }
  dispatch(action)
}
In Redux this is not the case. Instead, to actually initiate a dispatch, pass the result to the dispatch() function:
dispatch(addTodo(text))
dispatch(completeTodo(index))
*/

// MUTATE
const fetchPosts = subreddit => dispatch => {
  dispatch(requestPosts(subreddit))
  return fetch(`https://www.reddit.com/r/${subreddit}.json`)
    .then(response => response.json())
    .then(json => dispatch(receivePosts(subreddit, json)))
}

const shouldFetchPosts = (state, subreddit) => {
  const posts = state.postsBySubreddit[subreddit]
  if (!posts) {
    return true
  }
  if (posts.isFetching) {
    return false
  }
  return posts.didInvalidate
}

export const fetchPostsIfNeeded = subreddit => (dispatch, getState) => {
  if (shouldFetchPosts(getState(), subreddit)) {
    return dispatch(fetchPosts(subreddit))
  }
}
